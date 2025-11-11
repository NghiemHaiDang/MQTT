using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using ChatMQTT.Client.WPF.Models;

namespace ChatMQTT.Client.WPF.Services;

public class TopicSubscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly string _baseUrl;

    public static TopicSubscription? CurrentSubscription { get; set; }

    public TopicSubscriptionService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler);
        _authService = new AuthService();
        _baseUrl = $"{ConfigService.GetApiBaseUrl()}/api/TopicSubscription";
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

    public async Task<TopicSubscription?> SubscribeAsync(string topicId)
    {
        try
        {
            SetAuthorizationHeader();

            var request = new SubscribeTopicRequest
            {
                SubscriptionId = Guid.NewGuid().ToString(),
                DeviceId = ConfigService.GetDeviceId(),
                TopicId = topicId
            };

            var httpResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/subscribe", request);

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/subscribe", request);
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<SubscribeTopicResponse>();

            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to topic: {ex.Message}");
            return null;
        }
    }

    public async Task<TopicSubscription?> SubscribeUserAsync(string topicId, string deviceId)
    {
        try
        {
            SetAuthorizationHeader();

            var request = new SubscribeTopicRequest
            {
                SubscriptionId = Guid.NewGuid().ToString(),
                DeviceId = deviceId,
                TopicId = topicId
            };

            var httpResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/subscribe", request);

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/subscribe", request);
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<SubscribeTopicResponse>();

            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing user to topic: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UnsubscribeAsync(string subscriptionId)
    {
        try
        {
            SetAuthorizationHeader();

            var httpResponse = await _httpClient.DeleteAsync($"{_baseUrl}/unsubscribe/{subscriptionId}");

            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshTokenIfNeededAsync();
                if (refreshed)
                {
                    httpResponse = await _httpClient.DeleteAsync($"{_baseUrl}/unsubscribe/{subscriptionId}");
                }
            }

            httpResponse.EnsureSuccessStatusCode();

            var response = await httpResponse.Content.ReadFromJsonAsync<UnsubscribeTopicResponse>();

            return response?.Success == true && response.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error unsubscribing from topic: {ex.Message}");
            return false;
        }
    }
}
