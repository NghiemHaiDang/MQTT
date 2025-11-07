using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatMQTT.Client.WPF.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        var baseUrl = ConfigService.GetApiBaseUrl();
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl)
        };
    }

    public async Task<LoginData?> LoginAsync(string email, string password)
    {
        try
        {
            var loginRequest = new LoginRequest
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/Auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

                if (result?.Success == true && result.Data != null)
                {
                    return result.Data;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Login failed: {ex.Message}", ex);
        }
    }

    public async Task<RegisterResult?> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var registerRequest = new RegisterRequest
            {
                UserName = username,
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/Auth/register", registerRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();

                if (result?.Success == true)
                {
                    return new RegisterResult
                    {
                        Success = true,
                        Message = result.Message
                    };
                }
                else
                {
                    return new RegisterResult
                    {
                        Success = false,
                        Message = result?.Message ?? "Registration failed"
                    };
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new RegisterResult
                {
                    Success = false,
                    Message = $"Registration failed: {errorContent}"
                };
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Registration failed: {ex.Message}", ex);
        }
    }

    public async Task<LoginData?> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        try
        {
            var refreshRequest = new RefreshTokenRequest
            {
                Token = accessToken,
                RefreshToken = refreshToken
            };

            var response = await _httpClient.PostAsJsonAsync("/api/Auth/refresh-token", refreshRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

                if (result?.Success == true && result.Data != null)
                {
                    return result.Data;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Refresh token failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/Auth/logout", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LogoutResponse>();
                return result?.Success == true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout failed: {ex.Message}");
            return false;
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class RegisterResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public LoginData? Data { get; set; }
}

public class LoginData
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("accessTokenExpires")]
    public DateTime AccessTokenExpires { get; set; }

    [JsonPropertyName("refreshTokenExpires")]
    public DateTime RefreshTokenExpires { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
