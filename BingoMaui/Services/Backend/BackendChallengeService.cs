using System.Text;
using System.Text.Json;

namespace BingoMaui.Services.Backend
{
    public class BackendChallengeService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public BackendChallengeService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // ========= CardId-first (bakåtkompatibel) =========
        // identifier = CardId (GUID) → skickas som CardId
        // annars → skickas som ChallengeTitle (fallback för äldre data/klient)
        public async Task<bool> MarkChallengeAsCompletedAsync(string gameId, string identifier)
        {
            try
            {
                var payload = BuildActionPayload(gameId, identifier);
                using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/challenges/complete", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await ShowAlert("Session utgången", "Logga in igen.");
                    await AccountServices.LogoutAsync();
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                    Console.WriteLine($"❌ Complete misslyckades: {response.StatusCode}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fel vid complete: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnmarkChallengeAsCompletedAsync(string gameId, string identifier)
        {
            try
            {
                var payload = BuildActionPayload(gameId, identifier);
                using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/challenges/uncomplete", content);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await ShowAlert("Session utgången", "Logga in igen.");
                    await AccountServices.LogoutAsync();
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                    Console.WriteLine($"❌ Uncomplete misslyckades: {response.StatusCode}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fel vid uncomplete: {ex.Message}");
                return false;
            }
        }

        // ========= Tydliga helpers (rekommenderad användning) =========
        public Task<bool> MarkChallengeAsCompletedByCardIdAsync(string gameId, string cardId)
            => MarkChallengeAsCompletedAsync(gameId, cardId);

        public Task<bool> UnmarkChallengeAsCompletedByCardIdAsync(string gameId, string cardId)
            => UnmarkChallengeAsCompletedAsync(gameId, cardId);

        // ========= Random challenges (oförändrat bortsett från retry) =========
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

        // ========= Private helpers =========

        // Bygger payload som backend nu stödjer: { GameId, CardId? , ChallengeTitle? }
        private static object BuildActionPayload(string gameId, string identifier)
        {
            // CardId om det ser ut som ett GUID (vi genererar GUID i klienten; backend genererar annars)
            bool isGuid = Guid.TryParse(identifier, out _);

            if (isGuid)
                return new { GameId = gameId, CardId = identifier };

            // fallback: titel (för äldre klienter/spel)
            return new { GameId = gameId, ChallengeTitle = identifier };
        }

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
}
