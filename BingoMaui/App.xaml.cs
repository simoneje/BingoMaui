using BingoMaui.Services;
using BingoMaui.Services.Auth;
using BingoMaui.Services.Backend;
using Microsoft.Maui.Storage;

namespace BingoMaui
{
    public partial class App : Application
    {
        public static UserProfile CurrentUserProfile { get; set; }

        public static Dictionary<string, Dictionary<string, List<string>>> CompletedChallengesCache { get; private set; } = new();

        public static bool ShouldRefreshChallenges { get; set; } = false;

        public App()
        {
            InitializeComponent();
            MainPage = new SplashPage(); // Tillfällig sida medan init körs
        }

        public static async Task InitializeAsync()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("UserId");
                // 🔹 TESTKOD - simulera utgången token (Steg 3)
                // await SecureStorage.SetAsync("IdToken", "invalid.jwt.token");
                // lämna RefreshToken orörd
                var isLoggedInStr = await SecureStorage.GetAsync("IsLoggedIn");
                var isLoggedIn = bool.TryParse(isLoggedInStr, out var ok) && ok;

                if (!isLoggedIn || string.IsNullOrEmpty(userId)) return;

                var token = await SecureStorage.GetAsync("IdToken");

                // 1) Saknas eller är utgången? Försök refresh
                if (string.IsNullOrEmpty(token) || JwtService.IsTokenExpired(token))
                {
                    var refreshToken = await SecureStorage.GetAsync("RefreshToken");
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        var newToken = await FirebaseAuthService.RefreshIdTokenAsync(refreshToken);
                        if (string.IsNullOrEmpty(newToken))
                        {
                            await AccountServices.LogoutAsync();
                            return;
                        }
                        token = newToken;
                    }
                    else
                    {
                        await AccountServices.LogoutAsync();
                        return;
                    }
                }

                // 2) Sätt Bearer till backend
                BackendServices.UpdateToken(token);

                // 3) Ladda profil
                CurrentUserProfile = await BackendServices.MiscService.GetUserProfileFromApiAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App Init Error] {ex.Message}");
            }
        }


        //private static async Task LogCredentialFileAsync()
        //{
        //    string path = Path.Combine(FileSystem.AppDataDirectory, "credentials", "bingomaui28990.json");

        //    if (File.Exists(path))
        //        Console.WriteLine($"Credential file found. Size: {new FileInfo(path).Length} bytes");
        //    else
        //        Console.WriteLine("Credential file NOT found.");
        //}


    }
}
