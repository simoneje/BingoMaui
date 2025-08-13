using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BingoMaui.Services.Backend;

public class BackendChallengeService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public BackendChallengeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<bool> MarkChallengeAsCompletedAsync(string gameId, string challengeTitle)
    {
        var payload = new
        {
            GameId = gameId,
            ChallengeTitle = challengeTitle
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/challenges/complete", content);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await ShowAlert("Session utgången", "Logga in igen.");
            await AccountServices.LogoutAsync();
            return false;
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnmarkChallengeAsCompletedAsync(string gameId, string challengeTitle)
    {
        var payload = new
        {
            GameId = gameId,
            ChallengeTitle = challengeTitle
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/challenges/uncomplete", content);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await ShowAlert("Session utgången", "Logga in igen.");
            await AccountServices.LogoutAsync();
            return false;
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<List<Challenge>> GetRandomChallengesAsync(int count)
    {
        try
        {
            var response = await SendWithRetryAsync(() =>
                _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/challenges/random?count={count}")
            );

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return new List<Challenge>();
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    await ShowAlert("Inga utmaningar", "Vi kunde inte hitta några utmaningar just nu.");
                else if ((int)response.StatusCode >= 500)
                    await ShowAlert("Serverfel", "Något gick fel. Försök igen strax.");

                Console.WriteLine($"❌ Kunde inte hämta utmaningar: {response.StatusCode}");
                return new List<Challenge>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var challenges = JsonSerializer.Deserialize<List<Challenge>>(json, _jsonOptions);
            return challenges ?? new List<Challenge>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel vid hämtning av utmaningar: {ex.Message}");
            await ShowAlert("Nätverksfel", "Kunde inte hämta utmaningar. Kontrollera nätet och försök igen.");
            return new List<Challenge>();
        }
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
