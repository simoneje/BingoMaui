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
            var response = await _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/comments/{gameId}");


            if (!response.IsSuccessStatusCode)
            {
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
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fel vid post av kommentar: {ex.Message}");
            return false;
        }
    }
}
