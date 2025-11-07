using System.Windows;
using System.Windows.Input;
using ChatMQTT.Client.WPF.Services;

namespace ChatMQTT.Client.WPF.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly MainViewModel _mainViewModel;
    private readonly AuthService _authService;

    private string _loginEmail = "";
    private string _loginPassword = "";
    private string _registerUsername = "";
    private string _registerEmail = "";
    private string _registerPassword = "";
    private string _registerConfirmPassword = "";
    private string _loginError = "";
    private string _registerError = "";
    private bool _isLoginLoading;
    private bool _isRegisterLoading;
    private bool _isLoginView = true;

    public string LoginEmail
    {
        get => _loginEmail;
        set => SetProperty(ref _loginEmail, value);
    }

    public string LoginPassword
    {
        get => _loginPassword;
        set => SetProperty(ref _loginPassword, value);
    }

    public string RegisterUsername
    {
        get => _registerUsername;
        set => SetProperty(ref _registerUsername, value);
    }

    public string RegisterEmail
    {
        get => _registerEmail;
        set => SetProperty(ref _registerEmail, value);
    }

    public string RegisterPassword
    {
        get => _registerPassword;
        set => SetProperty(ref _registerPassword, value);
    }

    public string RegisterConfirmPassword
    {
        get => _registerConfirmPassword;
        set => SetProperty(ref _registerConfirmPassword, value);
    }

    public string LoginError
    {
        get => _loginError;
        set => SetProperty(ref _loginError, value);
    }

    public string RegisterError
    {
        get => _registerError;
        set => SetProperty(ref _registerError, value);
    }

    public bool IsLoginLoading
    {
        get => _isLoginLoading;
        set => SetProperty(ref _isLoginLoading, value);
    }

    public bool IsRegisterLoading
    {
        get => _isRegisterLoading;
        set => SetProperty(ref _isRegisterLoading, value);
    }

    public bool IsLoginView
    {
        get => _isLoginView;
        set => SetProperty(ref _isLoginView, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand ShowLoginCommand { get; }
    public ICommand ShowRegisterCommand { get; }

    public LoginViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _authService = new AuthService();

        LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoginLoading);
        RegisterCommand = new RelayCommand(async _ => await RegisterAsync(), _ => !IsRegisterLoading);
        ShowLoginCommand = new RelayCommand(_ => IsLoginView = true);
        ShowRegisterCommand = new RelayCommand(_ => IsLoginView = false);
    }

    private async Task LoginAsync()
    {
        LoginError = "";

        if (string.IsNullOrWhiteSpace(LoginEmail))
        {
            LoginError = "Please enter your email or username";
            return;
        }

        if (string.IsNullOrWhiteSpace(LoginPassword))
        {
            LoginError = "Please enter your password";
            return;
        }

        IsLoginLoading = true;

        try
        {
            var result = await _authService.LoginAsync(LoginEmail.Trim(), LoginPassword);

            if (result != null && !string.IsNullOrEmpty(result.AccessToken))
            {
                TokenService.AccessToken = result.AccessToken;
                TokenService.RefreshToken = result.RefreshToken;
                TokenService.AccessTokenExpires = result.AccessTokenExpires;
                _mainViewModel.NavigateToTopicSelection(result.UserName, result.UserId);
            }
            else
            {
                LoginError = "Invalid email/username or password. Please try again.";
            }
        }
        catch (Exception ex)
        {
            LoginError = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoginLoading = false;
        }
    }

    private async Task RegisterAsync()
    {
        RegisterError = "";

        if (string.IsNullOrWhiteSpace(RegisterUsername))
        {
            RegisterError = "Please enter a username";
            return;
        }

        if (string.IsNullOrWhiteSpace(RegisterEmail))
        {
            RegisterError = "Please enter your email address";
            return;
        }

        if (string.IsNullOrWhiteSpace(RegisterPassword))
        {
            RegisterError = "Please enter a password";
            return;
        }

        if (RegisterPassword.Length < 6)
        {
            RegisterError = "Password must be at least 6 characters long";
            return;
        }

        if (string.IsNullOrWhiteSpace(RegisterConfirmPassword))
        {
            RegisterError = "Please confirm your password";
            return;
        }

        if (RegisterPassword != RegisterConfirmPassword)
        {
            RegisterError = "Passwords do not match. Please try again.";
            return;
        }

        IsRegisterLoading = true;

        try
        {
            var result = await _authService.RegisterAsync(
                RegisterUsername.Trim(),
                RegisterEmail.Trim(),
                RegisterPassword);

            if (result != null && result.Success)
            {
                MessageBox.Show(
                    "Registration successful!\n\nYou can now login with your credentials.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                IsLoginView = true;
                LoginEmail = RegisterEmail;
                RegisterUsername = "";
                RegisterEmail = "";
                RegisterPassword = "";
                RegisterConfirmPassword = "";
            }
            else
            {
                RegisterError = result?.Message ?? "Registration failed. Please try again.";
            }
        }
        catch (Exception ex)
        {
            RegisterError = $"Registration failed: {ex.Message}";
        }
        finally
        {
            IsRegisterLoading = false;
        }
    }
}
