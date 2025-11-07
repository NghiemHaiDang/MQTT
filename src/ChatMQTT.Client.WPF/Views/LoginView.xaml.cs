using System.Windows;
using System.Windows.Controls;
using ChatMQTT.Client.WPF.ViewModels;

namespace ChatMQTT.Client.WPF.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void TxtLoginPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.LoginPassword = passwordBox.Password;
        }
    }

    private void TxtRegisterPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.RegisterPassword = passwordBox.Password;
        }
    }

    private void TxtRegisterConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.RegisterConfirmPassword = passwordBox.Password;
        }
    }
}
