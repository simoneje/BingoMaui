using BingoMaui.Services;
using BingoMaui.Services.Backend;
using Firebase.Auth;
namespace BingoMaui;

public partial class StartPage : ContentPage
{
    private readonly FirestoreService _firestoreService;
    public StartPage()
    {
        InitializeComponent();
        _firestoreService = new FirestoreService();

    }
    protected override async void OnAppearing()
    {
        string userId = Preferences.Get("UserId", string.Empty);
        if (App.CurrentUserProfile == null)
            App.CurrentUserProfile = new UserProfile();
        App.CurrentUserProfile = await BackendServices.MiscService.GetUserProfileFromApiAsync();
        // Använd global variabel för att visa nickname
        WelcomeLabel.Text = $"Välkommen, {App.CurrentUserProfile.Nickname}!";
    }

    private async void OnNavigateButtonClickedCreate(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CreateGame());
    }
    private async void OnNavigateButtonClickedStart(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new JoinGame());
    }
    private async void OnNavigateButtonClickedSettings(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SettingsPage());
    }
    private async void OnNavigateButtonClickedProfile(object sender, EventArgs e)
    {
        string userId = Preferences.Get("UserId", string.Empty);
        await Navigation.PushAsync(new ProfilePublicPage(userId));
    }
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logga ut", "Är du säker på att du vill logga ut?", "Ja", "Nej");
        if (!confirm)
            return;

        try
        {
            // Rensa inloggningsstatus
            Preferences.Clear();
            App.ClearLoggedInNickname();
;
            // Rensa den lokala cachen
            App.CompletedChallengesCache.Clear();

            AccountServices.ClearGameCacheOnLogout();

            Console.WriteLine("Cache cleared upon logout.");

            // Navigera användaren till LoginPage
            Application.Current.MainPage = new NavigationPage(new MainPage());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel vid utloggning: {ex.Message}");
            await DisplayAlert("Fel", "Det gick inte att logga ut. Försök igen.", "OK");
        }
    }
    private async void OnMyGamesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MyGames());
    }

}