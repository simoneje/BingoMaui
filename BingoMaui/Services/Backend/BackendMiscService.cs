using System.Net.Http.Headers;
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
            var response = await _httpClient.GetAsync("https://backendbingoapi.onrender.com/api/user/ping");
            return response.IsSuccessStatusCode;
        }


        public async Task<UserProfile> GetUserProfileFromApiAsync()
        {
            try
            {
                var token = Preferences.Get("IdToken", "");
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("https://backendbingoapi.onrender.com/api/profiles/profile");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Failed to fetch user profile from backend.");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var profile = JsonSerializer.Deserialize<UserProfile>(json);
                return profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user profile from API: {ex.Message}");
                return null;
            }
        }
    }
}


