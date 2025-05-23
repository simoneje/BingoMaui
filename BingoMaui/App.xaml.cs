using BingoMaui.Services;
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
                Task.Run(async () => await CopyServiceAccountKeyAsync()).Wait();

                await LogCredentialFileAsync();

                // 🔒 Hämta användarinfo från SecureStorage
                var userId = await SecureStorage.GetAsync("UserId");
                var isLoggedInStr = await SecureStorage.GetAsync("IsLoggedIn");
                var isLoggedIn = bool.TryParse(isLoggedInStr, out var result) && result;

                if (isLoggedIn && !string.IsNullOrEmpty(userId))
                {
                    // Ladda backend-token
                    await BackendServices.UpdateTokenAsync();

                    CurrentUserProfile = await BackendServices.MiscService.GetUserProfileFromApiAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App Init Error] {ex.Message}");
            }
        }

        private static async Task CopyServiceAccountKeyAsync()
        {
            string fileName = "bingomaui28990.json";
            string destPath = Path.Combine(FileSystem.AppDataDirectory, "credentials", fileName);

            if (!Directory.Exists(Path.Combine(FileSystem.AppDataDirectory, "credentials")))
                Directory.CreateDirectory(Path.Combine(FileSystem.AppDataDirectory, "credentials"));

            if (!File.Exists(destPath))
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
                using var outputStream = File.Create(destPath);
                await stream.CopyToAsync(outputStream);
            }

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", destPath);
        }

        private static async Task LogCredentialFileAsync()
        {
            string path = Path.Combine(FileSystem.AppDataDirectory, "credentials", "bingomaui28990.json");

            if (File.Exists(path))
                Console.WriteLine($"Credential file found. Size: {new FileInfo(path).Length} bytes");
            else
                Console.WriteLine("Credential file NOT found.");
        }

        public static void ClearLoggedInNickname()
        {
            CurrentUserProfile.Nickname = string.Empty;
        }
    }
}
