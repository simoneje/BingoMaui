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
    private async void OnClearCacheClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Rensa cache", "Vill du rensa alla sparade spel och lokal data?", "Rensa", "Avbryt");
        if (!confirm) return;

        AccountServices.ClearGameCacheOnLogout();
        App.CompletedChallengesCache.Clear();

        await DisplayAlert("Klart", "Cachen är rensad.", "OK");
    }
}
