using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using ChatMQTT.Client.WPF.Models;

namespace ChatMQTT.Client.WPF.Services;

public class TopicService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly string _baseUrl;

    public TopicService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
        _authService = new AuthService();
        _baseUrl = $"{ConfigService.GetApiBaseUrl()}/api/Topic";
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

    public async Task<List<Topic>> GetTopicsAsync(int pageIndex = 1, int pageSize = 100)
    {
        try
        {
            SetAuthorizationHeader();

            var httpResponse = await _httpClient.GetAsync(
                $"{_baseUrl}/paged?pageIndex={pageIndex}&pageSize={pageSize}");

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.GetAsync(
                        $"{_baseUrl}/paged?pageIndex={pageIndex}&pageSize={pageSize}");
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<PagedTopicResponse>();

            if (response?.Success == true && response.Data?.Count > 0)
            {
                return response.Data.SelectMany(list => list).ToList();
            }

            return new List<Topic>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting topics: {ex.Message}");
            return new List<Topic>();
        }
    }

    public async Task<CreateTopicResponse?> CreateTopicAsync(CreateTopicRequest request)
    {
        try
        {
            SetAuthorizationHeader();

            var httpResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/create", request);

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/create", request);
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            return await httpResponse.Content.ReadFromJsonAsync<CreateTopicResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating topic: {ex.Message}");
            return null;
        }
    }

    public async Task<Topic?> GetTopicByIdAsync(string topicId)
    {
        try
        {
            SetAuthorizationHeader();

            var httpResponse = await _httpClient.GetAsync($"{_baseUrl}/{topicId}");

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.GetAsync($"{_baseUrl}/{topicId}");
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<TopicDetailResponse>();

            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting topic details: {ex.Message}");
            return null;
        }
    }
}
