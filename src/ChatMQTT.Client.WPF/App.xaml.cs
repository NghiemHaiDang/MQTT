using System.Windows;
using ChatMQTT.Client.WPF.Services;

namespace ChatMQTT.Client.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = ConfigService.GetSettings();

        System.Console.WriteLine($"App started with DeviceId: {settings.DeviceId}");
        System.Console.WriteLine($"API Base URL: {settings.ApiBaseUrl}");
    }
}
