using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using BuildingBlocks.Models.Clients;
using MQTTnet;
using MQTTnet.Client;

namespace ChatMQTT.Client.WPF;

public partial class MainWindow : Window
{
    private IMqttClient _mqttClient;
    private string _currentTopic = "chat/general";
    private string _username = "";
    private string _clientId = "";
    private ObservableCollection<ChatMessage> _messages;

    public MainWindow()
    {
        InitializeComponent();
        _messages = new ObservableCollection<ChatMessage>();
        lstMessages.ItemsSource = _messages;

        _mqttClient = new MqttFactory().CreateMqttClient();
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
    }

    private async void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        if (_mqttClient.IsConnected)
        {
            await DisconnectAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            MessageBox.Show("Please enter a username!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _username = txtUsername.Text.Trim();
        _clientId = $"{_username}_{Guid.NewGuid().ToString()[..8]}";

        try
        {
            var host = txtHost.Text;
            var port = int.Parse(txtPort.Text);

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(host, port)
                .WithClientId(_clientId)
                .Build();

            await _mqttClient.ConnectAsync(options);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(_currentTopic))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions);

            UpdateConnectionStatus(true);
            _messages.Clear();

            MessageBox.Show("Connected successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DisconnectAsync()
    {
        try
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
            }
            UpdateConnectionStatus(false);
            MessageBox.Show("Disconnected successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Disconnect failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        if (isConnected)
        {
            txtStatus.Text = "Connected";
            txtStatus.Foreground = System.Windows.Media.Brushes.Green;
            txtClientId.Text = _clientId;
            txtCurrentTopic.Text = $"Topic: {_currentTopic}";
            btnConnect.Content = "Disconnect";
            btnConnect.Background = System.Windows.Media.Brushes.Red;
            txtMessage.IsEnabled = true;
            btnSend.IsEnabled = true;
            txtHost.IsEnabled = false;
            txtPort.IsEnabled = false;
            txtUsername.IsEnabled = false;
        }
        else
        {
            txtStatus.Text = "Disconnected";
            txtStatus.Foreground = System.Windows.Media.Brushes.Red;
            txtClientId.Text = "-";
            txtCurrentTopic.Text = "Topic: (Not connected)";
            btnConnect.Content = "Connect";
            btnConnect.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#27AE60"));
            txtMessage.IsEnabled = false;
            btnSend.IsEnabled = false;
            txtHost.IsEnabled = true;
            txtPort.IsEnabled = true;
            txtUsername.IsEnabled = true;
        }
    }

    private async void BtnSend_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void TxtMessage_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        if (!_mqttClient.IsConnected || string.IsNullOrWhiteSpace(txtMessage.Text))
            return;

        try
        {
            var chatMessage = new ChatMessage
            {
                From = _username,
                Message = txtMessage.Text.Trim(),
                Timestamp = DateTime.Now,
                Topic = _currentTopic
            };

            var json = JsonSerializer.Serialize(chatMessage);
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(_currentTopic)
                .WithPayload(json)
                .Build();

            await _mqttClient.PublishAsync(message);
            txtMessage.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to send message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                var chatMessage = JsonSerializer.Deserialize<ChatMessage>(payload);

                if (chatMessage != null)
                {
                    _messages.Add(chatMessage);
                    txtMessageCount.Text = _messages.Count.ToString();

                    scrollViewer.ScrollToBottom();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        });

        return Task.CompletedTask;
    }

    private async void BtnChangeTopic_Click(object sender, RoutedEventArgs e)
    {
        if (!_mqttClient.IsConnected)
        {
            MessageBox.Show("Please connect first!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var newTopic = txtTopic.Text.Trim();
        if (string.IsNullOrWhiteSpace(newTopic))
        {
            MessageBox.Show("Please enter a valid topic!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _mqttClient.UnsubscribeAsync(_currentTopic);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(newTopic))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions);

            _currentTopic = newTopic;
            txtCurrentTopic.Text = $"Topic: {_currentTopic}";
            _messages.Clear();
            txtMessageCount.Text = "0";

            MessageBox.Show($"Switched to topic: {newTopic}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to change topic: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
        }
        base.OnClosing(e);
    }
}