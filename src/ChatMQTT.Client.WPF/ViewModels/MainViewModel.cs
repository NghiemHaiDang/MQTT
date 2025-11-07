using System.Windows;

namespace ChatMQTT.Client.WPF.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel? _currentViewModel;

    public BaseViewModel? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public MainViewModel()
    {
        CurrentViewModel = new LoginViewModel(this);
    }

    public void NavigateToTopicSelection(string username, string userId)
    {
        CurrentViewModel = new TopicSelectionViewModel(username, userId, this);
    }

    public void NavigateToChat(string username, string userId, string topicName, string topicId)
    {
        CurrentViewModel = new ChatViewModel(username, userId, topicName, topicId, this);
    }

    public void NavigateToLogin()
    {
        CurrentViewModel = new LoginViewModel(this);
    }
}
