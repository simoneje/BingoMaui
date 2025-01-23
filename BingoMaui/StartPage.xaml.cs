using BingoMaui.Services;
namespace BingoMaui;

public partial class StartPage : ContentPage
{
	public StartPage()
	{
		InitializeComponent();
	}

    private async void OnNavigateButtonClickedCreate(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CreateGame());
    }
    private async void OnNavigateButtonClickedStart(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new JoinGame());
    }
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        // Rensa inloggningsstatus
        Preferences.Clear();

        // Navigera användaren till LoginPage
        Application.Current.MainPage = new NavigationPage(new MainPage());

        // Eventuellt: Bekräfta utloggning
        await DisplayAlert("Logout", "Du har loggats ut.", "OK");
    }
    private async void OnMyGamesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MyGames());
    }

}