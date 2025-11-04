using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using BuildingBlocks.Models.Servers;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace ChatMQTT.Server.WPF;

public class TopicInfo
{
    public string Topic { get; set; } = "";
    public int SubscriberCount { get; set; }
}

public partial class MainWindow : Window
{
    private MqttServer? _mqttServer;
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _topicSubscribers = new();
    private int _totalMessages = 0;
    private DateTime? _startTime;
    private DispatcherTimer? _uptimeTimer;
    private ObservableCollection<ClientInfo> _clientsList;
    private ObservableCollection<TopicInfo> _topicsList;

    public MainWindow()
    {
        InitializeComponent();
        _clientsList = new ObservableCollection<ClientInfo>();
        _topicsList = new ObservableCollection<TopicInfo>();
        dgClients.ItemsSource = _clientsList;
        lstTopics.ItemsSource = _topicsList;

        _uptimeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uptimeTimer.Tick += UpdateUptime;
    }

    private void UpdateUptime(object? sender, EventArgs e)
    {
        if (_startTime.HasValue)
        {
            var uptime = DateTime.Now - _startTime.Value;
            txtUptime.Text = $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
        }
    }

    private async void BtnStartStop_Click(object sender, RoutedEventArgs e)
    {
        if (_mqttServer != null && _mqttServer.IsStarted)
        {
            await StopServerAsync();
        }
        else
        {
            await StartServerAsync();
        }
    }

    private async Task StartServerAsync()
    {
        try
        {
            var port = int.Parse(txtPort.Text);

            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(port);

            _mqttServer = new MqttFactory().CreateMqttServer(optionsBuilder.Build());

            _mqttServer.ClientConnectedAsync += OnClientConnected;
            _mqttServer.ClientDisconnectedAsync += OnClientDisconnected;
            _mqttServer.ClientSubscribedTopicAsync += OnClientSubscribedTopic;
            _mqttServer.ClientUnsubscribedTopicAsync += OnClientUnsubscribedTopic;
            _mqttServer.InterceptingPublishAsync += OnInterceptingPublish;
            _mqttServer.ValidatingConnectionAsync += OnValidatingConnection;

            await _mqttServer.StartAsync();

            _startTime = DateTime.Now;
            _uptimeTimer?.Start();

            Dispatcher.Invoke(() =>
            {
                txtServerStatus.Text = "Running";
                txtServerStatus.Foreground = System.Windows.Media.Brushes.Green;
                btnStartStop.Content = "Stop Server";
                btnStartStop.Background = System.Windows.Media.Brushes.Red;
                txtPort.IsEnabled = false;
            });

            AddLog($"Server started on port {port}");
            MessageBox.Show($"MQTT Broker started successfully on port {port}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task StopServerAsync()
    {
        try
        {
            if (_mqttServer != null)
            {
                await _mqttServer.StopAsync();
                _mqttServer.Dispose();
                _mqttServer = null;
            }

            _uptimeTimer?.Stop();
            _clients.Clear();
            _topicSubscribers.Clear();

            Dispatcher.Invoke(() =>
            {
                txtServerStatus.Text = "Stopped";
                txtServerStatus.Foreground = System.Windows.Media.Brushes.Red;
                btnStartStop.Content = "Start Server";
                btnStartStop.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
                txtPort.IsEnabled = true;
                txtUptime.Text = "00:00:00";
                _clientsList.Clear();
                _topicsList.Clear();
            });

            AddLog("Server stopped");
            MessageBox.Show("MQTT Broker stopped successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to stop server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task OnValidatingConnection(ValidatingConnectionEventArgs args)
    {
        AddLog($"Client connecting: {args.ClientId}");
        return Task.CompletedTask;
    }

    private Task OnClientConnected(ClientConnectedEventArgs args)
    {
        var clientInfo = new ClientInfo
        {
            ClientId = args.ClientId,
            ConnectedAt = DateTime.Now,
            LastActivity = DateTime.Now,
            MessagesSent = 0,
            Endpoint = args.Endpoint ?? "Unknown"
        };

        _clients[args.ClientId] = clientInfo;

        Dispatcher.Invoke(() =>
        {
            _clientsList.Add(clientInfo);
            txtConnectedClients.Text = _clients.Count.ToString();
        });

        AddLog($"Client connected: {args.ClientId} from {args.Endpoint}");
        return Task.CompletedTask;
    }

    private Task OnClientDisconnected(ClientDisconnectedEventArgs args)
    {
        if (_clients.TryRemove(args.ClientId, out var clientInfo))
        {
            Dispatcher.Invoke(() =>
            {
                _clientsList.Remove(clientInfo);
                txtConnectedClients.Text = _clients.Count.ToString();
            });
        }

        foreach (var topic in _topicSubscribers.Keys.ToList())
        {
            if (_topicSubscribers.TryGetValue(topic, out var subscribers))
            {
                subscribers.Remove(args.ClientId);
                if (subscribers.Count == 0)
                {
                    _topicSubscribers.TryRemove(topic, out _);
                }
            }
        }

        UpdateTopicsList();
        AddLog($"Client disconnected: {args.ClientId} ({args.DisconnectType})");
        return Task.CompletedTask;
    }

    private Task OnClientSubscribedTopic(ClientSubscribedTopicEventArgs args)
    {
        var topic = args.TopicFilter.Topic;

        if (!_topicSubscribers.ContainsKey(topic))
        {
            _topicSubscribers[topic] = new HashSet<string>();
        }

        _topicSubscribers[topic].Add(args.ClientId);
        UpdateTopicsList();

        AddLog($"Client {args.ClientId} subscribed to: {topic}");
        return Task.CompletedTask;
    }

    private Task OnClientUnsubscribedTopic(ClientUnsubscribedTopicEventArgs args)
    {
        var topic = args.TopicFilter;

        if (_topicSubscribers.TryGetValue(topic, out var subscribers))
        {
            subscribers.Remove(args.ClientId);
            if (subscribers.Count == 0)
            {
                _topicSubscribers.TryRemove(topic, out _);
            }
        }

        UpdateTopicsList();
        AddLog($"Client {args.ClientId} unsubscribed from: {topic}");
        return Task.CompletedTask;
    }

    private Task OnInterceptingPublish(InterceptingPublishEventArgs args)
    {
        _totalMessages++;

        if (_clients.TryGetValue(args.ClientId, out var clientInfo))
        {
            clientInfo.MessagesSent++;
            clientInfo.LastActivity = DateTime.Now;

            Dispatcher.Invoke(() =>
            {
                dgClients.Items.Refresh();
            });
        }

        Dispatcher.Invoke(() =>
        {
            txtTotalMessages.Text = _totalMessages.ToString();
        });

        var payload = args.ApplicationMessage.PayloadSegment.Count > 0
            ? Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment)
            : "";

        AddLog($"Message published to '{args.ApplicationMessage.Topic}' by {args.ClientId}: {payload.Substring(0, Math.Min(50, payload.Length))}...");

        return Task.CompletedTask;
    }

    private void UpdateTopicsList()
    {
        Dispatcher.Invoke(() =>
        {
            _topicsList.Clear();
            foreach (var topic in _topicSubscribers)
            {
                _topicsList.Add(new TopicInfo
                {
                    Topic = topic.Key,
                    SubscriberCount = topic.Value.Count
                });
            }
            txtActiveTopics.Text = _topicsList.Count.ToString();
        });
    }

    private void AddLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLog.Text += $"[{timestamp}] {message}\n";
            scrollViewerLog.ScrollToBottom();
        });
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        txtLog.Text = "";
    }

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_mqttServer != null && _mqttServer.IsStarted)
        {
            await StopServerAsync();
        }
        base.OnClosing(e);
    }
}