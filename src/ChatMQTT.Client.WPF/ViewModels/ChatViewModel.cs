using ChatMQTT.Client.WPF.Models;
using ChatMQTT.Client.WPF.Services;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace ChatMQTT.Client.WPF.ViewModels;

public class ChatViewModel : BaseViewModel
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttSettings _mqttSettings;
    private readonly AuthService _authService;
    private readonly TopicSubscriptionService _subscriptionService;
    private readonly ChatService _chatService;
    private readonly MessageService _messageService;
    private readonly TopicService _topicService;
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
    public ICommand CreatePrivateChatCommand { get; }

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
        _topicService = new TopicService();
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
        CreatePrivateChatCommand = new RelayCommand(async user => await CreatePrivateChatAsync(user as ChatUser));

        _ = LoadChatUsersAsync();
        _ = LoadChatHistoryAsync();
        _ = ConnectAsync();
    }

    private async Task LoadChatUsersAsync()
    {
        try
        {
            Console.WriteLine($"[LoadChatUsers] Loading users for UserId={_userId}, TopicId={_currentTopicId}");
            var users = await _chatService.GetChatUsersAsync(_userId, _currentTopicId);

            Console.WriteLine($"[LoadChatUsers] Received {users.Count} users from API");

            ChatUsers.Clear();
            foreach (var user in users)
            {
                Console.WriteLine($"[LoadChatUsers] Adding user: {user.Username} (UserId={user.UserId})");
                ChatUsers.Add(user);
            }

            Console.WriteLine($"[LoadChatUsers] Total users in ChatUsers collection: {ChatUsers.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadChatUsers] ERROR: {ex.Message}");
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
            var deviceId = string.IsNullOrWhiteSpace(ConfigService.GetDeviceId())
                    ? $"Device_{Guid.NewGuid():N}"
                    : ConfigService.GetDeviceId();

            ClientId = deviceId;

            var options = new MqttClientOptionsBuilder()
                .WithClientId(ClientId)
                .WithTcpServer(_mqttSettings.Host, _mqttSettings.Port)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .Build();
            _mqttClient.DisconnectedAsync -= OnMqttClientDisconnected;
            _mqttClient.DisconnectedAsync += OnMqttClientDisconnected;

            await _mqttClient.ConnectAsync(options);
            Console.WriteLine($"[MQTT] Connected to {_mqttSettings.Host}:{_mqttSettings.Port}");
            if (!string.IsNullOrWhiteSpace(_currentTopicId))
            {
                Console.WriteLine($"[MQTT] Subscribing to TopicId={_currentTopicId}");
                await _mqttClient.SubscribeAsync(_currentTopicId);
                Console.WriteLine($"[MQTT] Successfully subscribed to topic: {_currentTopicId}");
            }
            else
            {
                Console.WriteLine("[MQTT] Warning: No topic specified for subscription");
            }

            IsConnected = true;
            ConnectionStatus = $"Connected to {_mqttSettings.Host}:{_mqttSettings.Port} (ClientId={ClientId})";
            Messages.Clear();
            MessageCount = 0;
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = "Connection failed";
            Console.WriteLine($"[MQTT] Connection error: {ex.Message}");
            MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OnMqttClientDisconnected(MqttClientDisconnectedEventArgs e)
    {
        Console.WriteLine($"[MQTT] Disconnected: {e.Reason}");
        IsConnected = false;
        ConnectionStatus = "Disconnected - retrying...";

        await Task.Delay(TimeSpan.FromSeconds(5));

        try
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(ClientId)
                .WithTcpServer(_mqttSettings.Host, _mqttSettings.Port)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .Build();

            await _mqttClient.ConnectAsync(options);
            if (!string.IsNullOrWhiteSpace(_currentTopicId))
            {
                await _mqttClient.SubscribeAsync(_currentTopicId);
                Console.WriteLine($"[MQTT] Reconnected and subscribed to TopicId={_currentTopicId}");
            }

            IsConnected = true;
            ConnectionStatus = "Reconnected";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ConnectionStatus = "Reconnect failed";
            Console.WriteLine($"[MQTT] Reconnect failed: {ex.Message}");
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

            var response = await _messageService.SendDirectMessageAsync(request);
            if (response == null)
            {
                MessageBox.Show("Failed to send message to API", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                var mqttPayload = JsonSerializer.Serialize(new ChatMessage
                {
                    From = Username,
                    Message = messageText,
                    Timestamp = DateTime.Now,
                    Topic = _currentTopic,
                    IsOwnMessage = false
                });

                var mqttMsg = new MqttApplicationMessageBuilder()
                    .WithTopic(_currentTopicId)
                    .WithPayload(mqttPayload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(mqttMsg);
                Console.WriteLine($"[MQTT] Published message to TopicId={_currentTopicId}: {mqttPayload}");
            }
            else
            {
                Console.WriteLine("[MQTT] Client not connected, skipping publish.");
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

    private async Task CreatePrivateChatAsync(ChatUser? selectedUser)
    {
        if (selectedUser == null)
            return;
        if (selectedUser.UserId == _userId)
        {
            MessageBox.Show("You cannot create a private chat with yourself.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var topicName = $"{Username}/{selectedUser.Username}";
            var createTopicRequest = new CreateTopicRequest
            {
                TopicId = Guid.NewGuid().ToString(),
                Name = topicName,
                Description = $"Private chat between {Username} and {selectedUser.Username}",
                AppId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                Status = 0,
                CreatedBy = _userId
            };
            var createResponse = await _topicService.CreateTopicAsync(createTopicRequest);

            if (createResponse == null || !createResponse.Success || createResponse.Data == null)
            {
                MessageBox.Show("Failed to create private chat topic.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var newTopic = createResponse.Data;
            Console.WriteLine($"[CreatePrivateChat] New topic created: TopicId={newTopic.TopicId}, Name={newTopic.Name}");

            Console.WriteLine($"[CreatePrivateChat] Subscribing current user (UserId={_userId}) to topic {newTopic.TopicId}");
            var currentUserSubscription = await _subscriptionService.SubscribeAsync(newTopic.TopicId);

            if (currentUserSubscription == null)
            {
                Console.WriteLine($"[CreatePrivateChat] ERROR: Failed to subscribe current user");
                MessageBox.Show("Failed to subscribe current user to the new topic.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Console.WriteLine($"[CreatePrivateChat] Current user subscribed: SubscriptionId={currentUserSubscription.SubscriptionId}");

            TopicSubscriptionService.CurrentSubscription = currentUserSubscription;

            Console.WriteLine($"[CreatePrivateChat] Subscribing selected user (UserId={selectedUser.UserId}) to topic {newTopic.TopicId}");
            var selectedUserSubscription = await _subscriptionService.SubscribeUserAsync(newTopic.TopicId, selectedUser.UserId);

            if (selectedUserSubscription == null)
            {
                Console.WriteLine($"[CreatePrivateChat] ERROR: Failed to subscribe selected user");
                MessageBox.Show($"Warning: Failed to subscribe {selectedUser.Username} to the topic. They may need to subscribe manually.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                Console.WriteLine($"[CreatePrivateChat] Selected user subscribed: SubscriptionId={selectedUserSubscription.SubscriptionId}");
            }

            MessageBox.Show($"Private chat created successfully with {selectedUser.Username}!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
            await Task.Delay(500);
            await Cleanup();
            _mainViewModel.NavigateToChat(Username, _userId, newTopic.Name, newTopic.TopicId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating private chat: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
