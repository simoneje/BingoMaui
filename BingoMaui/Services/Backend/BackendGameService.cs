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

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Misslyckades med att skapa spel: {response.StatusCode}");
                await ShowAlert("Kunde inte skapa spel", "Försök igen strax.");
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
            await ShowAlert("Nätverksfel", "Kunde inte skapa spel. Kolla nätet och försök igen.");
            return null;
        }
    }

    public async Task<BingoGame> JoinGameAsync(string inviteCode, string nickname, string playerColor)
    {
        var request = new JoinGameRequest
        {
            InviteCode = inviteCode,
            Nickname = nickname,
            PlayerColor = string.IsNullOrWhiteSpace(playerColor) ? "#00FFFF" : playerColor
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/games/join", content);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await ShowAlert("Session utgången", "Logga in igen.");
            await AccountServices.LogoutAsync();
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ JoinGame failed: {response.StatusCode}");
            await ShowAlert("Kunde inte gå med", "Koden kan vara fel eller nätet strular.");
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

        // 🧠 Ny: alltid hämta spelet från backend om Game saknas
        var game = result.Game ?? await GetGameByIdAsync(result.GameId);
        if (game != null)
        {
            AccountServices.SaveGameToCache(game);
            return game;
        }

        Console.WriteLine("⚠️ Kunde inte hämta spelet efter join.");
        return null;
    }

    public async Task<BingoGame> GetGameByIdAsync(string gameId)
    {
        var response = await SendWithRetryAsync(() =>
            _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/games/{gameId}")
        );

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await ShowAlert("Session utgången", "Logga in igen.");
            await AccountServices.LogoutAsync();
            return null;
        }

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BingoGame>(json, _jsonOptions);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            await ShowAlert("Hittades inte", "Spelet saknas eller är borttaget.");
        else if ((int)response.StatusCode >= 500)
            await ShowAlert("Serverfel", "Något gick fel. Försök igen strax.");

        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ Kunde inte hämta spelet: {error}");
        return null;
    }

    public async Task<List<BingoGame>> GetGamesForUserAsync()
    {
        try
        {
            var response = await SendWithRetryAsync(() =>
                _httpClient.GetAsync("https://backendbingoapi.onrender.com/api/games/user")
            );

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return new List<BingoGame>();
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    await ShowAlert("Inga spel", "Vi kunde inte hitta några spel för dig ännu.");
                else if ((int)response.StatusCode >= 500)
                    await ShowAlert("Serverfel", "Något gick fel. Försök igen strax.");

                Console.WriteLine($"❌ Misslyckades hämta spel: {response.StatusCode}");
                return new List<BingoGame>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<BingoGame>>(json, _jsonOptions) ?? new List<BingoGame>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel vid GetGamesForUserAsync: {ex.Message}");
            await ShowAlert("Nätverksfel", "Kunde inte ansluta. Kontrollera nätet och försök igen.");
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

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return false;
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel vid färgändring i spel: {ex.Message}");
            await ShowAlert("Fel", "Kunde inte ändra färg just nu.");
            return false;
        }
    }

    public class JoinGameResponse
    {
        public string Message { get; set; }
        public string GameId { get; set; }
        public BingoGame Game { get; set; }
    }

    // === Private helpers ===
    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> send, int maxAttempts = 3, CancellationToken ct = default)
    {
        var attempt = 0;
        var rnd = new Random();

        while (true)
        {
            attempt++;
            HttpResponseMessage? resp = null;
            try
            {
                resp = await send();
                if (IsTransient(resp.StatusCode) && attempt < maxAttempts)
                {
                    resp.Dispose();
                    await DelayWithExponentialBackoff(attempt, rnd, ct);
                    continue;
                }
                return resp;
            }
            catch (HttpRequestException)
            {
                if (attempt >= maxAttempts) throw;
                await DelayWithExponentialBackoff(attempt, rnd, ct);
            }
        }
    }

    private static bool IsTransient(System.Net.HttpStatusCode code)
        => code == System.Net.HttpStatusCode.RequestTimeout
        || code == (System.Net.HttpStatusCode)429
        || (int)code >= 500;

    private static Task DelayWithExponentialBackoff(int attempt, Random rnd, CancellationToken ct)
    {
        var baseMs = (int)(500 * Math.Pow(2, attempt - 1));
        var jitter = rnd.Next(0, 200);
        return Task.Delay(baseMs + jitter, ct);
    }

    private static Task ShowAlert(string title, string message)
    {
        return MainThread.InvokeOnMainThreadAsync(() =>
            Application.Current?.MainPage?.DisplayAlert(title, message, "OK") ?? Task.CompletedTask);
    }
}
