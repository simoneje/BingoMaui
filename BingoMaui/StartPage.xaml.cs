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
        

        // Anv�nd global variabel f�r att visa nickname
        WelcomeLabel.Text = $"V�lkommen, {App.LoggedInNickname}!";
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
        App.ClearLoggedInNickname(); // Anv�nd den nya metoden


        // Navigera anv�ndaren till LoginPage
        Application.Current.MainPage = new NavigationPage(new MainPage());

        // Eventuellt: Bekr�fta utloggning
        await DisplayAlert("Logout", "Du har loggats ut.", "OK");
    }
    private async void OnMyGamesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MyGames());
    }

}