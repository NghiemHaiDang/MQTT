namespace ChatMQTT.Client.WPF.Services;

public static class TokenService
{
    private static string? _accessToken;
    private static string? _refreshToken;
    private static DateTime _accessTokenExpires;

    public static string? AccessToken
    {
        get => _accessToken;
        set => _accessToken = value;
    }

    public static string? RefreshToken
    {
        get => _refreshToken;
        set => _refreshToken = value;
    }

    public static DateTime AccessTokenExpires
    {
        get => _accessTokenExpires;
        set => _accessTokenExpires = value;
    }

    public static bool IsTokenExpired()
    {
        return DateTime.UtcNow >= _accessTokenExpires.AddMinutes(-1);
    }

    public static void Clear()
    {
        _accessToken = null;
        _refreshToken = null;
        _accessTokenExpires = DateTime.MinValue;
    }
}
