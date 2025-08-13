using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BingoMaui.Services.Backend
{
    public class BackendMiscService
    {
        private readonly HttpClient _httpClient;

        public BackendMiscService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> TestPingAsync()
        {
            var response = await SendWithRetryAsync(() =>
                _httpClient.GetAsync("https://backendbingoapi.onrender.com/api/user/ping")
            );

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return false;
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<UserProfile> GetUserProfileFromApiAsync()
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                    _httpClient.GetAsync("https://backendbingoapi.onrender.com/api/profiles/profile")
                );

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await ShowAlert("Session utgången", "Logga in igen.");
                    await AccountServices.LogoutAsync();
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Failed to fetch user profile from backend.");
                    await ShowAlert("Fel", "Kunde inte hämta användarprofil.");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<UserProfile>(json);
                return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user profile from API: {ex.Message}");
                await ShowAlert("Nätverksfel", "Kunde inte hämta profil. Kontrollera nätet och försök igen.");
                return null;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(UserProfile profile)
        {
            var json = JsonSerializer.Serialize(profile);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/profile/update", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return false;
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<string> UploadProfileImageAsync(Stream imageStream, string filename)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(imageStream), "file", filename);

            var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/profile/upload-image", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Bildupload misslyckades: {response.StatusCode}");
                await ShowAlert("Fel", "Kunde inte ladda upp bilden.");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            return result != null && result.ContainsKey("imageUrl") ? result["imageUrl"] : null;
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
}
