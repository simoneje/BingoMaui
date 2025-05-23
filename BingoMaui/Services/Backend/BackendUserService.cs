using System.Net.Http.Json;
using System.Text.Json;


namespace BingoMaui.Services.Backend;

public class BackendUserService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BackendUserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserProfile> GetUserProfileAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/users/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Misslyckades hämta användare: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserProfile>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel vid GetUserProfileAsync: {ex.Message}");
            return null;
        }
    }

    // Lägg till fler metoder här
}
