using BingoMaui;
using BingoMaui.Services.Backend.RequestModels;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BingoMaui.Services.Backend;

public class BackendGameService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BackendGameService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<BingoGame> CreateGameAsync(CreateGameRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/games/create", content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Misslyckades med att skapa spel: {response.StatusCode}");
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var game = JsonSerializer.Deserialize<BingoGame>(responseJson, _jsonOptions);
            if (game == null)
            {
                Console.WriteLine("Problem with json deserialization");
                return null;
            }
            AccountServices.SaveGameToCache(game);
            return game;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel i CreateGameAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<BingoGame> JoinGameAsync(string inviteCode, string nickname, string playerColor)
    {
        var request = new JoinGameRequest
        {
            InviteCode = inviteCode,
            Nickname = nickname,
            PlayerColor = playerColor
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/games/join", content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ JoinGame failed: {response.StatusCode}");
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JoinGameResponse>(responseJson, _jsonOptions);

        if (result == null)
        {
            Console.WriteLine("❌ JoinGame: kunde inte deserialisera svaret.");
            return null;
        }

        if (result.Message == "Du är redan med i spelet." && !string.IsNullOrEmpty(result.GameId))
        {
            var cached = AccountServices.LoadGameFromCache(result.GameId);
            if (cached != null) return cached;

            var fallbackGame = await GetGameByIdAsync(result.GameId);
            if (fallbackGame != null)
            {
                AccountServices.SaveGameToCache(fallbackGame);
                return fallbackGame;
            }

            Console.WriteLine("⚠️ Kunde inte hämta spelet trots att vi redan var med.");
            return null;
        }

        if (result.Game != null)
        {
            AccountServices.SaveGameToCache(result.Game);
            return result.Game;
        }

        return null;
    }

    public async Task<BingoGame> GetGameByIdAsync(string gameId)
    {
        var response = await _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/games/{gameId}");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BingoGame>(json, _jsonOptions);
        }

        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ Kunde inte hämta spelet: {error}");
        return null;
    }

    public async Task<List<BingoGame>> GetGamesForUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://backendbingoapi.onrender.com/api/games/user");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Misslyckades hämta spel: {response.StatusCode}");
                return new List<BingoGame>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<BingoGame>>(json, _jsonOptions) ?? new List<BingoGame>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel vid GetGamesForUserAsync: {ex.Message}");
            return new List<BingoGame>();
        }
    }
    public async Task<bool> UpdatePlayerColorInGameAsync(string gameId, string newColor)
    {
        try
        {
            var payload = new { NewColor = newColor };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://backendbingoapi.onrender.com/api/games/{gameId}/update-color",
                content
            );

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel vid färgändring i spel: {ex.Message}");
            return false;
        }
    }

    public class JoinGameResponse
    {
        public string Message { get; set; }
        public string GameId { get; set; }
        public BingoGame Game { get; set; }
    }
}

    //public async Task<bool> UpdatePlayerColorAsync(string gameId, string newColor) { ... }
    //public async Task<bool> PostCommentAsync(string gameId, string message) { ... }
    //public async Task<List<Comment>> GetCommentsAsync(string gameId) { ... }
    //public async Task<Dictionary<string, int>> GetLeaderboardAsync(string gameId) { ... }

