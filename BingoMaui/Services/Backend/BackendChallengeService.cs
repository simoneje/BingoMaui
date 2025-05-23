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
        return response.IsSuccessStatusCode;
    }

    public async Task<List<Challenge>> GetRandomChallengesAsync(int count)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://backendbingoapi.onrender.com/api/challenges/random?count={count}");

            if (!response.IsSuccessStatusCode)
            {
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
            return new List<Challenge>();
        }
    }
}
