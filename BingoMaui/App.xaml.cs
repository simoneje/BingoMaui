using BingoMaui.Services;
namespace BingoMaui
{
    public partial class App : Application
    {
        public static string LoggedInNickname { get; set; }
        public static Dictionary<string, Dictionary<string, List<string>>> CompletedChallengesCache { get; private set; } =
            new Dictionary<string, Dictionary<string, List<string>>>();
        public App()
        {
            InitializeComponent();
            CopyServiceAccountKeyAsync();
            LogCredentialFileAsync();
            // Ladda nickname vid app-start
            var authService = new FirebaseAuthService();
            LoggedInNickname = authService.GetLoggedInNickname();
            Console.WriteLine(LoggedInNickname);

            // Kontrollera inloggningsstatus
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

            if (isLoggedIn)
            {
                // Om användaren är inloggad, skicka till StartPage
                MainPage = new NavigationPage(new StartPage());
                
            }
            else
            {
                // Om användaren inte är inloggad, sätt AppShell som huvudnavigering                
                MainPage = new AppShell();
            }
        }
        private async Task LogCredentialFileAsync()
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "credentials", "bingomaui28990.json");

            if (File.Exists(filePath))
            {
                var fileContent = await File.ReadAllTextAsync(filePath);
                Console.WriteLine($"JSON Key Content: {fileContent}");
            }
            else
            {
                Console.WriteLine("Credential file not found!");
            }
        }
        private async Task CopyServiceAccountKeyAsync()
        {
            string fileName = "bingomaui28990.json";
            string destPath = Path.Combine(FileSystem.AppDataDirectory, "credentials", fileName);

            try
            {
                if (!Directory.Exists(Path.Combine(FileSystem.AppDataDirectory, "credentials")))
                {
                    Directory.CreateDirectory(Path.Combine(FileSystem.AppDataDirectory, "credentials"));
                }

                if (!File.Exists(destPath))
                {
                    using (var stream = await FileSystem.OpenAppPackageFileAsync(fileName))

                    {
                        //Tog bort async från stream.CopyTo(outputStream);
                        using (var outputStream = File.Create(destPath))
                        {
                            stream.CopyTo(outputStream);
                            Console.WriteLine($"File copied to: {destPath}");
                        }
                    }
                }

                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", destPath);
                Console.WriteLine("Credential file copied and environment variable set.");
                if (File.Exists(destPath))
                {
                    var fileInfo = new FileInfo(destPath);
                    Console.WriteLine($"Credential file exists. Size: {fileInfo.Length} bytes");
                }
                else
                {
                    Console.WriteLine("Credential file not found after copying!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying credential file: {ex.Message}");
            }
        }
        public static void ClearLoggedInNickname()
        {
            LoggedInNickname = string.Empty;
        }
    }
}
