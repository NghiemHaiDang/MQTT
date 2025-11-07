using System.IO;
using System.Text.Json;
using ChatMQTT.Client.WPF.Models;

namespace ChatMQTT.Client.WPF.Services;

public class ConfigService
{
    private static AppSettings? _settings;
    private static readonly string ConfigTxtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
    private static readonly string ConfigJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

    public static AppSettings GetSettings()
    {
        if (_settings != null)
            return _settings;

        LoadFromTextConfig();

        LoadFromJsonConfig();

        _settings ??= new AppSettings();
        return _settings;
    }

    private static void LoadFromTextConfig()
    {
        try
        {
            if (!File.Exists(ConfigTxtPath))
            {
                CreateDefaultConfigTxt();
            }

            var lines = File.ReadAllLines(ConfigTxtPath);
            var configData = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    configData[key] = value;
                }
            }

            if (!configData.ContainsKey("DEVICE_ID") || string.IsNullOrWhiteSpace(configData["DEVICE_ID"]))
            {
                var deviceId = Guid.NewGuid().ToString();
                configData["DEVICE_ID"] = deviceId;
                UpdateConfigTxt("DEVICE_ID", deviceId);
            }

            var settings = new AppSettings
            {
                ApiBaseUrl = configData.ContainsKey("API_BASE_URL") ? configData["API_BASE_URL"] : "https://localhost:7063",
                DeviceId = configData["DEVICE_ID"],
                MqttSettings = new MqttSettings
                {
                    Host = configData.ContainsKey("MQTT_HOST") ? configData["MQTT_HOST"] : "localhost",
                    Port = configData.ContainsKey("MQTT_PORT") && int.TryParse(configData["MQTT_PORT"], out var port) ? port : 1883
                }
            };
            SaveToJsonConfig(settings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading from config.txt: {ex.Message}");
        }
    }

    private static void LoadFromJsonConfig()
    {
        try
        {
            if (File.Exists(ConfigJsonPath))
            {
                var json = File.ReadAllText(ConfigJsonPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading appsettings.json: {ex.Message}");
        }
    }

    private static void SaveToJsonConfig(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigJsonPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving appsettings.json: {ex.Message}");
        }
    }

    private static void CreateDefaultConfigTxt()
    {
        try
        {
            var defaultConfig = @"# API Configuration
API_BASE_URL=https://localhost:7063

# MQTT Configuration
MQTT_HOST=localhost
MQTT_PORT=1883

# Device ID (auto-generated on first run)
DEVICE_ID=
";
            File.WriteAllText(ConfigTxtPath, defaultConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating config.txt: {ex.Message}");
        }
    }

    private static void UpdateConfigTxt(string key, string value)
    {
        try
        {
            var lines = File.ReadAllLines(ConfigTxtPath).ToList();
            var updated = false;

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith(key + "="))
                {
                    lines[i] = $"{key}={value}";
                    updated = true;
                    break;
                }
            }

            if (!updated)
            {
                lines.Add($"{key}={value}");
            }

            File.WriteAllLines(ConfigTxtPath, lines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating config.txt: {ex.Message}");
        }
    }

    public static MqttSettings GetMqttSettings()
    {
        return GetSettings().MqttSettings;
    }

    public static string GetApiBaseUrl()
    {
        return GetSettings().ApiBaseUrl;
    }

    public static string GetDeviceId()
    {
        return GetSettings().DeviceId;
    }
}
