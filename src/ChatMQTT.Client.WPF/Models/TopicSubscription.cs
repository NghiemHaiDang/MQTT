using System.Text.Json.Serialization;

namespace ChatMQTT.Client.WPF.Models;

public class SubscribeTopicRequest
{
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("topicId")]
    public string TopicId { get; set; } = string.Empty;
}

public class TopicSubscription
{
    [JsonPropertyName("subscriptionId")]
    public string SubscriptionId { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("topicId")]
    public string TopicId { get; set; } = string.Empty;

    [JsonPropertyName("subscribedAt")]
    public DateTime SubscribedAt { get; set; }
}

public class SubscribeTopicResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public TopicSubscription? Data { get; set; }
}

public class UnsubscribeTopicResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public bool Data { get; set; }
}
