using System.Windows;
using System.Windows.Controls;
using ChatMQTT.Client.WPF.Models;
using ChatMQTT.Client.WPF.ViewModels;

namespace ChatMQTT.Client.WPF.Views;

public partial class TopicSelectionView : UserControl
{
    public TopicSelectionView()
    {
        InitializeComponent();
    }

    private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is Topic topic && DataContext is TopicSelectionViewModel viewModel)
        {
            viewModel.SelectedTopic = topic;
            if (viewModel.SelectTopicCommand.CanExecute(null))
            {
                viewModel.SelectTopicCommand.Execute(null);
            }
        }
    }
}
