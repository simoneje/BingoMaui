using BingoMaui.Services;
namespace BingoMaui;

public partial class DevToolsPage : ContentPage
{
    private readonly FirestoreService _firestoreService = new();
    private readonly FirestoreAdminService _adminService = new();

    public DevToolsPage()
    {

        InitializeComponent();
    }

    private async void OnMigratePlayerIdsClicked(object sender, EventArgs e)
    {
        await _adminService.MigratePlayerIdsAsync();
        await DisplayAlert("Klar", "PlayerIds migrerade!", "OK");
    }

    private async void OnShowUserInfoClicked(object sender, EventArgs e)
    {
        var uid = Preferences.Get("UserId", "N/A");
        var profile = App.CurrentUserProfile;

        string info = $"UserId: {uid}\nFärg: {profile?.PlayerColor ?? "?"}";
        await DisplayAlert("Användarinformation", info, "OK");
    }

    private void OnClearLocalCacheClicked(object sender, EventArgs e)
    {
        App.CompletedChallengesCache.Clear();
        DisplayAlert("Cache rensad", "App.CompletedChallengesCache är nu tom.", "OK");
    }
}