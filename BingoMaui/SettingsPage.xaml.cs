using BingoMaui.Services;
namespace BingoMaui;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnProfileSettingsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfileSettingsPage());
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        // Rensa lagrad info och gå tillbaka till inloggningssidan
        Preferences.Clear(); // Tar bort sparad UserId och Nickname
        App.CurrentUserProfile.Nickname = null;

        await Navigation.PushAsync(new StartPage());
    }
}
