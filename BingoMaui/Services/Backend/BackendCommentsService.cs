using System.Text;
using System.Text.Json;

namespace BingoMaui.Services.Backend;

public class BackendCommentsService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BackendCommentsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Comment>> GetCommentsAsync(string gameId)
    {
        try
        {
            var response = await SendWithRetryAsync(() =>
                _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/comments/{gameId}")
            );

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return new List<Comment>();
            }

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    await ShowAlert("Inga kommentarer", "Det finns inga kommentarer för det här spelet.");
                else if ((int)response.StatusCode >= 500)
                    await ShowAlert("Serverfel", "Något gick fel. Försök igen strax.");

                Console.WriteLine($"❌ Kunde inte hämta kommentarer: {response.StatusCode}");
                return new List<Comment>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var comments = JsonSerializer.Deserialize<List<Comment>>(json, _jsonOptions);

            return comments ?? new List<Comment>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel vid hämtning av kommentarer: {ex.Message}");
            await ShowAlert("Nätverksfel", "Kunde inte hämta kommentarer. Kontrollera nätet och försök igen.");
            return new List<Comment>();
        }
    }

    public async Task<bool> PostCommentAsync(string gameId, string message)
    {
        try
        {
            var payload = new
            {
                GameId = gameId,
                Message = message,
                Nickname = App.CurrentUserProfile?.Nickname ?? "Okänd"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/comments", content);

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
            Console.WriteLine($"❌ Fel vid post av kommentar: {ex.Message}");
            await ShowAlert("Fel", "Kunde inte skicka kommentaren. Försök igen.");
            return false;
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
