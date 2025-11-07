using System.Windows;
using ChatMQTT.Client.WPF.ViewModels;

namespace ChatMQTT.Client.WPF;

public partial class Shell : Window
{
    public Shell()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
