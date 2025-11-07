using System.Text.Json.Serialization;

namespace ChatMQTT.Client.WPF.Models;

public class ChatUser
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }

    public string Initial => !string.IsNullOrEmpty(Username) ? Username[0].ToString().ToUpper() : "?";
}

public class GetChatUsersResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<ChatUser> Data { get; set; } = new();
}
