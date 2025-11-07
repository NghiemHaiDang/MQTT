using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using ChatMQTT.Client.WPF.Models;

namespace ChatMQTT.Client.WPF.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly string _baseUrl;

    public ChatService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
        _authService = new AuthService();
        _baseUrl = $"{ConfigService.GetApiBaseUrl()}/api/Chat";
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

    public async Task<List<ChatUser>> GetChatUsersAsync(string userId, string topicId)
    {
        try
        {
            SetAuthorizationHeader();

            var httpResponse = await _httpClient.GetAsync($"{_baseUrl}/user/{userId}/topic/{topicId}");

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.GetAsync($"{_baseUrl}/user/{userId}/topic/{topicId}");
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<GetChatUsersResponse>();

            return response?.Success == true ? response.Data : new List<ChatUser>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting chat users: {ex.Message}");
            return new List<ChatUser>();
        }
    }

    public async Task<ChatHistoryResponse?> GetChatHistoryAsync(string topicId, int pageIndex = 1, int pageSize = 10)
    {
        try
        {
            SetAuthorizationHeader();

            var httpResponse = await _httpClient.GetAsync($"{_baseUrl}/history?PageIndex={pageIndex}&PageSize={pageSize}&TopicId={topicId}");

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.GetAsync($"{_baseUrl}/history?PageIndex={pageIndex}&PageSize={pageSize}&TopicId={topicId}");
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<ChatHistoryResponse>();

            return response?.Success == true ? response : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting chat history: {ex.Message}");
            return null;
        }
    }
}
