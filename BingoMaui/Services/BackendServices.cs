using BingoMaui.Services.Backend;
using System.Net.Http.Headers;

public static class BackendServices
{
    private static readonly HttpClient SharedHttpClient;
    private const string UserIdKey = "UserId";
    static BackendServices()
    {
        SharedHttpClient = new HttpClient();

        GameService = new BackendGameService(SharedHttpClient);
        ChallengeService = new BackendChallengeService(SharedHttpClient);
        UserService = new BackendUserService(SharedHttpClient);
        ProfileService = new BackendProfileService(SharedHttpClient);
        MiscService = new BackendMiscService(SharedHttpClient);
        CommentsService = new BackendCommentsService(SharedHttpClient);
    }

    public static BackendGameService GameService { get; }
    public static BackendChallengeService ChallengeService { get; }
    public static BackendUserService UserService { get; }
    public static BackendProfileService ProfileService { get; }
    public static BackendMiscService MiscService { get; }
    public static BackendCommentsService CommentsService { get; }
    public static HttpClient HttpClient => SharedHttpClient;

    public static void UpdateToken(string newToken)
    {
        SharedHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", newToken);
    }

    public static void ResetToken()
    {
        SharedHttpClient.DefaultRequestHeaders.Authorization = null;
    }

    public static async Task UpdateTokenAsync()
    {
        var token = await SecureStorage.GetAsync("IdToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            UpdateToken(token);
        }
    }
    public static async Task<string> GetUserIdAsync()
    {
        return await SecureStorage.GetAsync(UserIdKey) ?? string.Empty;
    }

    public static async Task SetUserIdAsync(string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            await SecureStorage.SetAsync(UserIdKey, userId);
    }

    public static void ClearUserId()
    {
        SecureStorage.Remove(UserIdKey);
    }


}

