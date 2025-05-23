using BingoMaui.Services;
using BingoMaui.Services.Backend;

namespace BingoMaui;
public partial class ProfileSettingsPage : ContentPage
{
    private UserProfile _currentUserProfile;
    private string _selectedColor = string.Empty;
    private Button _lastSelectedButton = null;

    public ProfileSettingsPage()
    {
        InitializeComponent();
        var colors = new List<string>
        {
            "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF"
        };

        ColorPickerCollectionView.ItemsSource = colors;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var profile = await BackendServices.ProfileService.GetUserProfileAsync();
        if (profile == null)
        {
            await DisplayAlert("Fel", "Kunde inte ladda din profil.", "OK");
            return;
        }

        App.CurrentUserProfile = profile;
        _currentUserProfile = profile;
        NicknameEntry.Text = profile.Nickname;
        _selectedColor = profile.PlayerColor;
        ColorPickerCollectionView.SelectedItem = _selectedColor;
    }

    private async void OnEditProfileClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfileEditPage());
    }

#if DEBUG
    private async void OnDevButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new DevToolsPage());
    }
#endif

    private void OnColorButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            string selectedColor = btn.BackgroundColor.ToHex();
            _selectedColor = selectedColor;

            if (_lastSelectedButton != null)
            {
                _lastSelectedButton.BorderColor = Colors.Transparent;
                _lastSelectedButton.BorderWidth = 0;
            }

            btn.BorderColor = Colors.Aqua;
            btn.BorderWidth = 3;
            _lastSelectedButton = btn;
        }
    }
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var newNickname = NicknameEntry.Text.Trim();
        var colorToSave = _selectedColor;

        if (string.IsNullOrEmpty(newNickname))
        {
            await DisplayAlert("Fel", "Du måste ange ett nickname!", "OK");
            return;
        }

        if (string.IsNullOrEmpty(colorToSave))
        {
            await DisplayAlert("Fel", "Du måste välja en färg!", "OK");
            return;
        }

        // Skicka till backend
        var nicknameUpdated = await BackendServices.ProfileService.UpdateNicknameAsync(newNickname);
        var colorUpdated = await BackendServices.ProfileService.UpdateColorAsync(colorToSave);

        // Synka nickname till alla spel
        if (nicknameUpdated)
            await BackendServices.ProfileService.SyncNicknameInAllGames(newNickname);

        // Uppdatera lokalt
        if (nicknameUpdated)
        {
            App.CurrentUserProfile.Nickname = newNickname;
            Preferences.Set("Nickname", newNickname);
        }

        if (colorUpdated)
        {
            App.CurrentUserProfile.PlayerColor = colorToSave;
        }

        if (nicknameUpdated || colorUpdated)
        {
            await DisplayAlert("Sparat", "Dina Profilinställningar har sparats", "OK");
        }
        else
        {
            await DisplayAlert("Fel", "Kunde inte spara profilinställningar.", "OK");
        }
    }

}
