namespace BingoMaui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            CopyServiceAccountKeyAsync();


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
        private async Task CopyServiceAccountKeyAsync()
        {
            string fileName = "bingomaui28990.json";
            string destPath = Path.Combine(FileSystem.AppDataDirectory, "credentials", fileName);

            // Skapa målkatalogen om den inte finns
            if (!Directory.Exists(Path.Combine(FileSystem.AppDataDirectory, "credentials")))
            {
                Directory.CreateDirectory(Path.Combine(FileSystem.AppDataDirectory, "credentials"));
            }

            // Kopiera filen
            if (!File.Exists(destPath))
            {
                using (var stream = await FileSystem.OpenAppPackageFileAsync(fileName))
                using (var outputStream = File.Create(destPath))
                {
                    await stream.CopyToAsync(outputStream);
                }
            }

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", destPath);
        }
    }
}
