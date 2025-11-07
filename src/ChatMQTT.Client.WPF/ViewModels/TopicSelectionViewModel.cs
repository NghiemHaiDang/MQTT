using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ChatMQTT.Client.WPF.Models;
using ChatMQTT.Client.WPF.Services;

namespace ChatMQTT.Client.WPF.ViewModels;

public class TopicSelectionViewModel : BaseViewModel
{
    private readonly MainViewModel _mainViewModel;
    private readonly TopicService _topicService;
    private readonly AuthService _authService;
    private readonly TopicSubscriptionService _subscriptionService;
    private readonly string _username;
    private readonly string _userId;

    private Topic? _selectedTopic;
    private bool _isLoading;
    private bool _isDetailDialogOpen;
    private Topic? _topicDetail;

    public ObservableCollection<Topic> Topics { get; }

    public Topic? SelectedTopic
    {
        get => _selectedTopic;
        set => SetProperty(ref _selectedTopic, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set => SetProperty(ref _isDetailDialogOpen, value);
    }

    public Topic? TopicDetail
    {
        get => _topicDetail;
        set => SetProperty(ref _topicDetail, value);
    }

    public string Username => _username;

    public ICommand SelectTopicCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ViewTopicDetailCommand { get; }
    public ICommand CloseDetailDialogCommand { get; }

    public TopicSelectionViewModel(string username, string userId, MainViewModel mainViewModel)
    {
        _username = username;
        _userId = userId;
        _mainViewModel = mainViewModel;
        _topicService = new TopicService();
        _authService = new AuthService();
        _subscriptionService = new TopicSubscriptionService();

        Topics = new ObservableCollection<Topic>();

        SelectTopicCommand = new RelayCommand(async _ => await SelectTopicAsync(), _ => SelectedTopic != null);
        RefreshCommand = new RelayCommand(async _ => await LoadTopicsAsync());
        LogoutCommand = new RelayCommand(async _ => await LogoutAsync());
        ViewTopicDetailCommand = new RelayCommand<Topic>(async topic => await ViewTopicDetailAsync(topic));
        CloseDetailDialogCommand = new RelayCommand(_ => IsDetailDialogOpen = false);

        _ = LoadTopicsAsync();
    }

    private async Task LoadTopicsAsync()
    {
        IsLoading = true;

        try
        {
            var topics = await _topicService.GetTopicsAsync();

            Topics.Clear();
            foreach (var topic in topics)
            {
                Topics.Add(topic);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load topics: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SelectTopicAsync()
    {
        if (SelectedTopic == null) return;

        IsLoading = true;

        try
        {
            var subscription = await _subscriptionService.SubscribeAsync(SelectedTopic.TopicId);

            if (subscription != null)
            {
                TopicSubscriptionService.CurrentSubscription = subscription;
                _mainViewModel.NavigateToChat(_username, _userId, SelectedTopic.Name, SelectedTopic.TopicId);
            }
            else
            {
                MessageBox.Show("Failed to subscribe to topic", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error subscribing to topic: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ViewTopicDetailAsync(Topic? topic)
    {
        if (topic == null) return;

        IsLoading = true;

        try
        {
            var topicDetail = await _topicService.GetTopicByIdAsync(topic.TopicId);

            if (topicDetail != null)
            {
                TopicDetail = topicDetail;
                IsDetailDialogOpen = true;
            }
            else
            {
                MessageBox.Show("Failed to load topic details", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading topic details: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
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
            IsLoading = true;

            try
            {
                await _authService.LogoutAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
            }
            finally
            {
                TokenService.Clear();
                _mainViewModel.NavigateToLogin();
                IsLoading = false;
            }
        }
    }
}
