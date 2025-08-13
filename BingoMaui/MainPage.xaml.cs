
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
                Application.Current.MainPage = new NavigationPage(new StartPage());
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
                // 🧹 Rensa gammal info
                await SecureStorage.SetAsync("IsLoggedIn", "false");
                SecureStorage.Remove("IdToken");
                SecureStorage.Remove("UserId");

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

    }
}
