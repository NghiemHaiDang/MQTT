using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ChatMQTT.Client.WPF.Services;

namespace ChatMQTT.Client.WPF;

public partial class LoginWindow : Window
{
    private readonly AuthService _authService;
    private string _loggedInUsername = string.Empty;

    public string LoggedInUsername => _loggedInUsername;

    public LoginWindow()
    {
        InitializeComponent();
        _authService = new AuthService();

        MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) DragMove(); };

        Loaded += (s, e) => txtLoginEmail.Focus();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnLoginTab_Click(object sender, RoutedEventArgs e)
    {
        ShowLoginView();
    }

    private void BtnRegisterTab_Click(object sender, RoutedEventArgs e)
    {
        ShowRegisterView();
    }

    private void ShowLoginView()
    {
        var storyboard = (Storyboard)FindResource("FadeIn");
        pnlLogin.BeginStoryboard(storyboard);

        pnlLogin.Visibility = Visibility.Visible;
        pnlRegister.Visibility = Visibility.Collapsed;

        btnLoginTab.Background = (SolidColorBrush)FindResource("SurfaceBrush");
        btnLoginTab.Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush");
        btnLoginTab.FontWeight = FontWeights.SemiBold;

        btnRegisterTab.Background = Brushes.Transparent;
        btnRegisterTab.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");
        btnRegisterTab.FontWeight = FontWeights.Normal;

        txtRegisterUsername.Clear();
        txtRegisterEmail.Clear();
        txtRegisterPassword.Clear();
        txtRegisterConfirmPassword.Clear();
        borderRegisterError.Visibility = Visibility.Collapsed;

        txtLoginEmail.Focus();
    }

    private void ShowRegisterView()
    {
        var storyboard = (Storyboard)FindResource("FadeIn");
        pnlRegister.BeginStoryboard(storyboard);

        pnlLogin.Visibility = Visibility.Collapsed;
        pnlRegister.Visibility = Visibility.Visible;

        btnLoginTab.Background = Brushes.Transparent;
        btnLoginTab.Foreground = (SolidColorBrush)FindResource("TextSecondaryBrush");
        btnLoginTab.FontWeight = FontWeights.Normal;

        btnRegisterTab.Background = (SolidColorBrush)FindResource("SurfaceBrush");
        btnRegisterTab.Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush");
        btnRegisterTab.FontWeight = FontWeights.SemiBold;

        txtLoginEmail.Clear();
        txtLoginPassword.Clear();
        borderLoginError.Visibility = Visibility.Collapsed;

        txtRegisterUsername.Focus();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        await LoginAsync();
    }

    private void TxtLoginPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = LoginAsync();
        }
    }

    private async Task LoginAsync()
    {
        borderLoginError.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(txtLoginEmail.Text))
        {
            ShowLoginError("Please enter your email or username");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtLoginPassword.Password))
        {
            ShowLoginError("Please enter your password");
            return;
        }

        btnLogin.IsEnabled = false;
        pnlLoginLoading.Visibility = Visibility.Visible;

        try
        {
            var result = await _authService.LoginAsync(txtLoginEmail.Text.Trim(), txtLoginPassword.Password);

            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                _loggedInUsername = result.UserName;

                MessageBox.Show($"Login successful! Welcome {_loggedInUsername}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                ShowLoginError("Invalid email/username or password. Please try again.");
            }
        }
        catch (Exception ex)
        {
            ShowLoginError($"Login failed: {ex.Message}");
        }
        finally
        {
            btnLogin.IsEnabled = true;
            pnlLoginLoading.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowLoginError(string message)
    {
        txtLoginError.Text = message;
        borderLoginError.Visibility = Visibility.Visible;
    }

    private async void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        await RegisterAsync();
    }

    private void TxtRegisterConfirmPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = RegisterAsync();
        }
    }

    private async Task RegisterAsync()
    {
        borderRegisterError.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(txtRegisterUsername.Text))
        {
            ShowRegisterError("Please enter a username");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtRegisterEmail.Text))
        {
            ShowRegisterError("Please enter your email address");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtRegisterPassword.Password))
        {
            ShowRegisterError("Please enter a password");
            return;
        }

        if (txtRegisterPassword.Password.Length < 6)
        {
            ShowRegisterError("Password must be at least 6 characters long");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtRegisterConfirmPassword.Password))
        {
            ShowRegisterError("Please confirm your password");
            return;
        }

        if (txtRegisterPassword.Password != txtRegisterConfirmPassword.Password)
        {
            ShowRegisterError("Passwords do not match. Please try again.");
            return;
        }

        btnRegister.IsEnabled = false;
        pnlRegisterLoading.Visibility = Visibility.Visible;

        try
        {
            var result = await _authService.RegisterAsync(
                txtRegisterUsername.Text.Trim(),
                txtRegisterEmail.Text.Trim(),
                txtRegisterPassword.Password);

            if (result != null && result.Success)
            {
                MessageBox.Show(
                    "Registration successful!\n\nYou can now login with your credentials.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ShowLoginView();
                txtLoginEmail.Text = txtRegisterEmail.Text;
            }
            else
            {
                ShowRegisterError(result?.Message ?? "Registration failed. Please try again.");
            }
        }
        catch (Exception ex)
        {
            ShowRegisterError($"Registration failed: {ex.Message}");
        }
        finally
        {
            btnRegister.IsEnabled = true;
            pnlRegisterLoading.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowRegisterError(string message)
    {
        txtRegisterError.Text = message;
        borderRegisterError.Visibility = Visibility.Visible;
    }
}
