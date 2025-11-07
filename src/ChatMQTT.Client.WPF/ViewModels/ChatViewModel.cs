using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using ChatMQTT.Client.WPF.Models;
using ChatMQTT.Client.WPF.Services;
using MQTTnet;
using MQTTnet.Client;

namespace ChatMQTT.Client.WPF.ViewModels;

public class ChatViewModel : BaseViewModel
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttSettings _mqttSettings;
    private readonly AuthService _authService;
    private readonly TopicSubscriptionService _subscriptionService;
    private readonly ChatService _chatService;
    private readonly MessageService _messageService;
    private string _username;
    private string _userId;
    private string _currentTopic = "chat/general";
    private string _currentTopicId = "";
    private string _newTopic = "chat/general";
    private string _message = "";
    private string _clientId = "";
    private string _connectionStatus = "Disconnected";
    private bool _isConnected;
    private int _messageCount;
    private int _currentPage = 1;
    private int _totalPages = 1;
    private bool _isLoadingHistory;
    private const int PageSize = 10;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string CurrentTopic
    {
        get => _currentTopic;
        set => SetProperty(ref _currentTopic, value);
    }

    public string NewTopic
    {
        get => _newTopic;
        set => SetProperty(ref _newTopic, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string ClientId
    {
        get => _clientId;
        set => SetProperty(ref _clientId, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public int MessageCount
    {
        get => _messageCount;
        set => SetProperty(ref _messageCount, value);
    }

    public bool IsLoadingHistory
    {
        get => _isLoadingHistory;
        set => SetProperty(ref _isLoadingHistory, value);
    }

    public ObservableCollection<ChatMessage> Messages { get; }
    public ObservableCollection<ChatUser> ChatUsers { get; }

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand ChangeTopicCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand UnsubscribeCommand { get; }
    public ICommand LoadMoreHistoryCommand { get; }

    private readonly MainViewModel _mainViewModel;

    public ChatViewModel(string username, string userId, string topicName, string topicId, MainViewModel mainViewModel)
    {
        _username = username;
        _userId = userId;
        _currentTopic = topicName;
        _currentTopicId = topicId;
        _newTopic = topicName;
        _mainViewModel = mainViewModel;
        _mqttSettings = ConfigService.GetMqttSettings();
        _authService = new AuthService();
        _subscriptionService = new TopicSubscriptionService();
        _chatService = new ChatService();
        _messageService = new MessageService();
        Messages = new ObservableCollection<ChatMessage>();
        ChatUsers = new ObservableCollection<ChatUser>();

        _mqttClient = new MqttFactory().CreateMqttClient();
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

        ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => !IsConnected);
        DisconnectCommand = new RelayCommand(async _ => await DisconnectAsync(), _ => IsConnected);
        SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => !string.IsNullOrWhiteSpace(Message));
        ChangeTopicCommand = new RelayCommand(async _ => await ChangeTopicAsync(), _ => IsConnected);
        LogoutCommand = new RelayCommand(async _ => await LogoutAsync());
        BackCommand = new RelayCommand(async _ => await BackToTopicsAsync());
        UnsubscribeCommand = new RelayCommand(async _ => await UnsubscribeAsync(), _ => TopicSubscriptionService.CurrentSubscription != null);
        LoadMoreHistoryCommand = new RelayCommand(async _ => await LoadChatHistoryAsync());

        // Load chat users và history
        _ = LoadChatUsersAsync();
        _ = LoadChatHistoryAsync();
    }

    private async Task LoadChatUsersAsync()
    {
        try
        {
            var users = await _chatService.GetChatUsersAsync(_userId, _currentTopicId);

            ChatUsers.Clear();
            foreach (var user in users)
            {
                ChatUsers.Add(user);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading chat users: {ex.Message}");
        }
    }

    private async Task LoadChatHistoryAsync()
    {
        if (IsLoadingHistory || _currentPage > _totalPages) return;

        IsLoadingHistory = true;

        try
        {
            var response = await _chatService.GetChatHistoryAsync(_currentTopicId, _currentPage, PageSize);

            if (response != null && response.Data.Count > 0)
            {
                _totalPages = (int)Math.Ceiling((double)response.TotalCount / PageSize);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    int insertIndex = 0;
                    foreach (var historyMsg in response.Data.OrderByDescending(m => m.CreatedAt))
                    {
                        var chatMessage = new ChatMessage
                        {
                            From = historyMsg.SenderName,
                            Message = historyMsg.Body,
                            Timestamp = historyMsg.CreatedAt.ToLocalTime(),
                            Topic = _currentTopic,
                            IsOwnMessage = historyMsg.SenderId == _userId
                        };

                        Messages.Insert(insertIndex, chatMessage);
                    }

                    MessageCount = Messages.Count;
                });

                _currentPage++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading chat history: {ex.Message}");
        }
        finally
        {
            IsLoadingHistory = false;
        }
    }

    public async Task OnScrollToTop()
    {
        await LoadChatHistoryAsync();
    }

    private async Task UnsubscribeAsync()
    {
        if (TopicSubscriptionService.CurrentSubscription == null) return;

        var result = MessageBox.Show(
            "Are you sure you want to unsubscribe from this topic?",
            "Confirm Unsubscribe",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var success = await _subscriptionService.UnsubscribeAsync(TopicSubscriptionService.CurrentSubscription.SubscriptionId);

                if (success)
                {
                    TopicSubscriptionService.CurrentSubscription = null;
                    MessageBox.Show("Unsubscribed successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await Cleanup();
                    _mainViewModel.NavigateToTopicSelection(_username, _userId);
                }
                else
                {
                    MessageBox.Show("Failed to unsubscribe", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unsubscribe error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task BackToTopicsAsync()
    {
        await Cleanup();
        _mainViewModel.NavigateToTopicSelection(_username, _userId);
    }

    private async Task LogoutAsync()
    {
        var result = MessageBox.Show(
            "Are you sure you want to logout?",
            "Confirm Logout",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await Cleanup();

            try
            {
                await _authService.LogoutAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
            }

            TokenService.Clear();
            _mainViewModel.NavigateToLogin();
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            ClientId = $"{Username}_{Guid.NewGuid().ToString()[..8]}";

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttSettings.Host, _mqttSettings.Port)
                .WithClientId(ClientId)
                .Build();

            await _mqttClient.ConnectAsync(options);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(CurrentTopic))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions);

            IsConnected = true;
            ConnectionStatus = $"Connected to {_mqttSettings.Host}:{_mqttSettings.Port}";
            Messages.Clear();
            MessageCount = 0;

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

            IsConnected = false;
            ConnectionStatus = "Disconnected";
            ClientId = "-";

            MessageBox.Show("Disconnected successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Disconnect failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(Message))
            return;

        var messageText = Message.Trim();

        try
        {
            var request = new SendDirectMessageRequest
            {
                TopicId = _currentTopicId,
                SenderId = _userId,
                Type = "text",
                Title = _currentTopic,
                Body = messageText,
                ImageUrl = string.Empty,
                TargetDeviceId = null
            };
            Application.Current.Dispatcher.Invoke(() =>
            {
                var chatMessage = new ChatMessage
                {
                    From = Username,
                    Message = messageText,
                    Timestamp = DateTime.Now,
                    Topic = _currentTopic,
                    IsOwnMessage = true
                };

                Messages.Add(chatMessage);
                MessageCount = Messages.Count;
            });
            Message = "";

            var response = await _messageService.SendDirectMessageAsync(request);

            if (response == null)
            {
                MessageBox.Show("Failed to send message", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to send message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ChangeTopicAsync()
    {
        if (!_mqttClient.IsConnected)
        {
            MessageBox.Show("Please connect first!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTopic))
        {
            MessageBox.Show("Please enter a valid topic!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _mqttClient.UnsubscribeAsync(CurrentTopic);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(NewTopic))
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions);

            CurrentTopic = NewTopic;
            Messages.Clear();
            MessageCount = 0;

            MessageBox.Show($"Switched to topic: {NewTopic}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to change topic: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            var chatMessage = JsonSerializer.Deserialize<ChatMessage>(payload);

            if (chatMessage != null)
            {
                chatMessage.IsOwnMessage = chatMessage.From == Username;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!chatMessage.IsOwnMessage)
                    {
                        Messages.Add(chatMessage);
                        MessageCount = Messages.Count;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public async Task Cleanup()
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
        }
        _mqttClient?.Dispose();
    }
}
