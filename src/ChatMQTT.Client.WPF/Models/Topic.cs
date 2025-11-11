using System.Text.Json.Serialization;

namespace ChatMQTT.Client.WPF.Models;

public class Topic
{
    [JsonPropertyName("topicId")]
    public string TopicId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("appId")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

public class PagedTopicResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<List<Topic>> Data { get; set; } = new();
}

public class CreateTopicRequest
{
    [JsonPropertyName("topicId")]
    public string TopicId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("appId")]
    public string AppId { get; set; } = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    [JsonPropertyName("status")]
    public int Status { get; set; } = 0;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class CreateTopicResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Topic? Data { get; set; }
}

public class TopicDetailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Topic? Data { get; set; }
}
