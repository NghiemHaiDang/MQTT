namespace ChatMQTT.Client.WPF.Models;

public class MqttSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
}

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = "https://localhost:7063";
    public string DeviceId { get; set; } = string.Empty;
    public MqttSettings MqttSettings { get; set; } = new();
}
