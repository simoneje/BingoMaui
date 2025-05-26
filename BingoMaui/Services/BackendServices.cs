using BingoMaui.Services.Backend;
using System.Net.Http.Headers;

public static class BackendServices
{
    private static readonly HttpClient SharedHttpClient;
    static BackendServices()
    {
        SharedHttpClient = new HttpClient();
        var token = Preferences.Get("IdToken", "");
        if (!string.IsNullOrEmpty(token))
        {
            SharedHttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

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

}

