using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BingoMaui.Services.Backend.RequestModels;

namespace BingoMaui.Services.Backend;

public class BackendProfileService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BackendProfileService(HttpClient client)
    {
        _httpClient = client;
    }

    public async Task<UserProfile> GetUserProfileAsync()
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
            Console.WriteLine($"❌ Misslyckades hämta profil: {response.StatusCode}");
            await ShowAlert("Fel", "Kunde inte hämta profil.");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);
    }

    public async Task<UserProfile> GetProfileByIdAsync(string userId)
    {
        var response = await SendWithRetryAsync(() =>
            _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/profiles/{userId}")
        );

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await ShowAlert("Session utgången", "Logga in igen.");
            await AccountServices.LogoutAsync();
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Kunde inte hämta profil för {userId}: {response.StatusCode}");
            await ShowAlert("Fel", "Kunde inte hämta användarprofil.");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);
    }

    public async Task<bool> UpdateNicknameAsync(string newNickname)
    {
        var payload = new UpdateNicknameRequest { NewNickname = newNickname };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/profiles/nickname", content);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await ShowAlert("Session utgången", "Logga in igen.");
            await AccountServices.LogoutAsync();
            return false;
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateColorAsync(string newColor)
    {
        var payload = new UpdateColorRequest { NewColor = newColor };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/profiles/color", content);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await ShowAlert("Session utgången", "Logga in igen.");
            await AccountServices.LogoutAsync();
            return false;
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SyncNicknameInAllGames(string newNickname)
    {
        try
        {
            var payload = new UpdateNicknameBatchRequest { NewNickname = newNickname };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/profiles/sync-nickname", content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await ShowAlert("Session utgången", "Logga in igen.");
                await AccountServices.LogoutAsync();
                return false;
            }

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Nickname uppdaterat i alla spel.");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Kunde inte synka nickname: {response.StatusCode}");
                await ShowAlert("Fel", "Kunde inte synka nickname.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel i SyncNicknameInAllGames: {ex.Message}");
            await ShowAlert("Nätverksfel", "Kunde inte synka nickname. Kontrollera nätet och försök igen.");
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
