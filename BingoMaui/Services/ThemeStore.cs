using System.Text.Json;
using BingoMaui.Services.Models;
public static class ThemeStore
{
    private const string Prefix = "game_theme_";
    private static readonly JsonSerializerOptions _opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static BoardTheme LoadForGame(string gameId)
    {
        var key = Prefix + gameId;
        if (!Preferences.ContainsKey(key)) return Default;
        var json = Preferences.Get(key, "");
        return string.IsNullOrWhiteSpace(json) ? Default
             : JsonSerializer.Deserialize<BoardTheme>(json, _opts) ?? Default;
    }

    public static void SaveForGame(string gameId, BoardTheme theme)
    {
        var json = JsonSerializer.Serialize(theme, _opts);
        Preferences.Set(Prefix + gameId, json);
    }

    public static BoardTheme Default => new BoardTheme
    {
        PageBackground = "#FFFFFF",
        TileBackground = "#6D28D9",
        TileText = "#FFFFFF",
        BadgeBackground = "#00000099"
    };

    public static bool TryParseHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return false;
        hex = hex.Trim();
        if (!hex.StartsWith("#")) return false;
        var core = hex[1..];
        return core.Length is 3 or 4 or 6 or 8
            && core.All(c => Uri.IsHexDigit(c));
    }
}
