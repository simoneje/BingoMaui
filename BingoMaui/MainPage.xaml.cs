using Google.Cloud.Firestore;
using System.Reflection;
using Microsoft.Maui.Storage;
                using System.IO;
using System.Text;
using static Google.Rpc.Context.AttributeContext.Types;
using Firebase.Auth;
using BingoMaui.Services;
using BingoMaui.Services.Backend;



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
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            try
            {
                var authService = new FirebaseAuthService();
                var token = await authService.LoginUserAsync(email, password); // 🔁 Returnerar ID-token

                if (!token.StartsWith("Error"))
                {
                    // 🔐 Spara token lokalt
                    await SecureStorage.SetAsync("IdToken", token);
                    await SecureStorage.SetAsync("IsLoggedIn", "true");
                    await BackendServices.UpdateTokenAsync(token);

                    // 🔄 Testa token mot backend
                    var api = BackendServices.MiscService; // ✅

                    var backendOk = await api.TestPingAsync();

                    if (backendOk)
                    {
                        await DisplayAlert("Success", "Du är inloggad och ansluten till Game Servern!", "OK");

                        // Navigera till StartPage om allt är OK
                        Application.Current.MainPage = new NavigationPage(new StartPage());
                    }
                    else
                    {
                        await DisplayAlert("Fel", "Inloggning lyckades, men anslutning till backend misslyckades (401?)", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error", token, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Ett fel uppstod: {ex.Message}", "OK");
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
