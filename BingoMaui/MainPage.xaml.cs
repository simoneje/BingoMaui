
using static Google.Rpc.Context.AttributeContext.Types;
using Firebase.Auth;
using BingoMaui.Services;
using BingoMaui.Services.Backend;
using System;
using BingoMaui.Services.Auth;



namespace BingoMaui
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
            CopyServiceAccountKeyAsync();
            

        }
        private async void OnMyGamesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MyGames());
        }
        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;
            var nickname = NicknameEntry.Text; // Kan vara null

            // Kalla på vår FirebaseAuthService
            var authService = new FirebaseAuthService();
            var result = await authService.RegisterUserAsync(email, password, nickname);

            if (!result.StartsWith("Error"))
            {
                await DisplayAlert("Success", "User registered!", "OK");
            }
            else
            {
                await DisplayAlert("Error", result, "OK");
            }
        }
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim();
            var password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Fel", "Fyll i både e-post och lösenord.", "OK");
                return;
            }

            try
            {
                var authService = new FirebaseAuthService();
                var idToken = await authService.LoginUserAsync(email, password); // 🔁 Får ID-token

                if (string.IsNullOrWhiteSpace(idToken) || idToken.StartsWith("Error"))
                {
                    await DisplayAlert("Fel", "Inloggning misslyckades. Kontrollera dina uppgifter.", "OK");
                    return;
                }

                // 🔐 Spara i SecureStorage
                await SecureStorage.SetAsync("IdToken", idToken);
                await SecureStorage.SetAsync("IsLoggedIn", "true");

                var decodedUserId = JwtService.ExtractUidFromToken(idToken);
                await SecureStorage.SetAsync("UserId", decodedUserId);

                // ⚙️ Uppdatera token globalt
                await BackendServices.UpdateTokenAsync();

                // ⛓️ Hämta profil från backend
                var profile = await BackendServices.MiscService.GetUserProfileFromApiAsync();
                if (profile == null)
                {
                    await DisplayAlert("Fel", "Kunde inte hämta användarprofil.", "OK");
                    return;
                }

                App.CurrentUserProfile = profile;

                // ✅ Navigera till start
                await DisplayAlert("Välkommen", $"Hej {profile.Nickname}!", "OK");
                Application.Current.MainPage = new NavigationPage(new StartPage());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                await DisplayAlert("Fel", "Ett oväntat fel inträffade. Försök igen.", "OK");
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

        //private async void OnTestFirestoreClicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        // Kontrollera om miljövariabeln är korrekt inställd
        //        string credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        //        if (string.IsNullOrEmpty(credentialsPath))
        //        {
        //            Console.WriteLine("GOOGLE_APPLICATION_CREDENTIALS is not set!");
        //            await DisplayAlert("Error", "GOOGLE_APPLICATION_CREDENTIALS is not set!", "OK");
        //            return;
        //        }

        //        Console.WriteLine($"GOOGLE_APPLICATION_CREDENTIALS is set to: {credentialsPath}");

        //        // Testa att skapa en Firestore-databasanslutning
        //        FirestoreDb db = FirestoreDb.Create("bingomaui28990");
        //        Console.WriteLine("Firestore connection succeeded!");

        //        await DisplayAlert("Success", "Connected to Firestore successfully!", "OK");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Firestore connection failed: {ex.Message}");
        //        await DisplayAlert("Error", $"Firestore connection failed: {ex.Message}", "OK");
        //        string credentialsPath = Path.Combine(FileSystem.AppDataDirectory, "Credentials", "bingomaui28990.json");
        //        if (File.Exists(credentialsPath))
        //        {
        //            Console.WriteLine($"Credential file exists at: {credentialsPath}");
        //        }
        //        else
        //        {
        //            Console.WriteLine($"Credential file NOT found at: {credentialsPath}");
        //        }
        //        if (File.Exists(credentialsPath))
        //        {
        //            string fileContent = File.ReadAllText(credentialsPath);
        //            Console.WriteLine($"File content length: {fileContent.Length}");
        //            Console.WriteLine($"File content: {fileContent}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Credential file not found!");
        //        }
        //        string googleCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        //        if (!string.IsNullOrEmpty(googleCredentials))
        //        {
        //            Console.WriteLine($"GOOGLE_APPLICATION_CREDENTIALS is set to: {googleCredentials}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("GOOGLE_APPLICATION_CREDENTIALS is not set!");
        //        }
        //    }
        //}
        private async void TestCredentialButton_Clicked(object sender, EventArgs e)
        {
            var path = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            await DisplayAlert("Path", path ?? "NULL", "OK");

            bool exists = File.Exists(path);
            await DisplayAlert("Exists?", exists.ToString(), "OK");
        }
    }
}
