namespace ChatMQTT.Client.WPF.Models;

public class ChatMessage
{
    public string From { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Topic { get; set; } = string.Empty;
    public bool IsOwnMessage { get; set; }

    public string FromInitial => !string.IsNullOrEmpty(From) ? From[0].ToString().ToUpper() : "?";
}
