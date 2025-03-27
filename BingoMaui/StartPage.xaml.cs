using BingoMaui.Services;
namespace BingoMaui;

public partial class StartPage : ContentPage
{
    public StartPage()
    {
        InitializeComponent();

    }
    protected override void OnAppearing()
    {


        // Använd global variabel för att visa nickname
        WelcomeLabel.Text = $"Välkommen, {App.LoggedInNickname}!";
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
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logga ut", "Är du säker på att du vill logga ut?", "Ja", "Nej");
        if (!confirm)
            return;

        try
        {
            // Rensa inloggningsstatus
            Preferences.Clear();
            App.ClearLoggedInNickname(); // Använd den nya metoden;

            // Rensa den lokala cachen
            App.CompletedChallengesCache.Clear();

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