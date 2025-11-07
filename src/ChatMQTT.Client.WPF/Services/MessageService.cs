using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using ChatMQTT.Client.WPF.Models;

namespace ChatMQTT.Client.WPF.Services;

public class MessageService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly string _baseUrl;

    public MessageService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
        _authService = new AuthService();
        _baseUrl = $"{ConfigService.GetApiBaseUrl()}/api/Message";
    }

    private void SetAuthorizationHeader()
    {
        if (!string.IsNullOrEmpty(TokenService.AccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TokenService.AccessToken);
        }
    }

    private async Task<bool> RefreshTokenIfNeededAsync()
    {
        if (!string.IsNullOrEmpty(TokenService.RefreshToken))
        {
            var newTokenData = await _authService.RefreshTokenAsync(
                TokenService.AccessToken ?? string.Empty,
                TokenService.RefreshToken);

            if (newTokenData != null)
            {
                TokenService.AccessToken = newTokenData.AccessToken;
                TokenService.RefreshToken = newTokenData.RefreshToken;
                TokenService.AccessTokenExpires = newTokenData.AccessTokenExpires;
                SetAuthorizationHeader();
                return true;
            }
        }
        return false;
    }

    public async Task<SendDirectMessageResponse?> SendDirectMessageAsync(SendDirectMessageRequest request)
    {
        try
        {
            SetAuthorizationHeader();

            var httpResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/messages/direct", request);
            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/messages/direct", request);
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<SendDirectMessageResponse>();

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending direct message: {ex.Message}");
            return null;
        }
    }
}
