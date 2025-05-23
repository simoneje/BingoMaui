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
        var response = await _httpClient.GetAsync("https://backendbingoapi.onrender.com/api/profiles/profile");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Misslyckades hämta profil: {response.StatusCode}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);
    }

    public async Task<UserProfile> GetProfileByIdAsync(string userId)
    {
        var response = await _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/profiles/{userId}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"❌ Kunde inte hämta profil för {userId}: {response.StatusCode}");
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
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateColorAsync(string newColor)
    {
        var payload = new UpdateColorRequest { NewColor = newColor };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/profiles/color", content);
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
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Nickname uppdaterat i alla spel.");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Kunde inte synka nickname: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel i SyncNicknameInAllGames: {ex.Message}");
            return false;
        }
    }
    //public async Task<bool> UpdatePlayerColorAsync(string newColor)
    //{
    //    var payload = new { NewColor = newColor };
    //    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    //    var response = await _httpClient.PostAsync("https://backendbingoapi.onrender.com/api/user/updateColor", content);
    //    return response.IsSuccessStatusCode;
    //}

}
