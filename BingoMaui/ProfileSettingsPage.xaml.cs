using BingoMaui.Services;
namespace BingoMaui;
public partial class ProfileSettingsPage : ContentPage
{
    private readonly FirestoreService _firestoreService;
    private string _userId;
    private UserProfile _currentUserProfile;
    private string _selectedColor = string.Empty; // Håller den valda färgen
    private Button _lastSelectedButton = null;      // Håller referensen till den tidigare klickade knappen
    public ProfileSettingsPage()
    {
        InitializeComponent();
        _firestoreService = new FirestoreService();
        _userId = Preferences.Get("UserId", string.Empty);
        var colors = new List<string>
            {
                "#FF5733", "#33FF57", "#3357FF", "#FF33A1",
                "#A133FF", "#33FFF2", "#F2FF33", "#FF8F33"
            };

        ColorPickerCollectionView.ItemsSource = colors;
        LoadUserProfile();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var userId = Preferences.Get("UserId", string.Empty);
        if (string.IsNullOrEmpty(userId))
        {
            await DisplayAlert("Fel", "Inget användar-ID hittades.", "OK");
            return;
        }

        // Hämta användarprofilen från Firestore (skapa/utöka en metod GetUserProfileAsync i FirestoreService)
        _currentUserProfile = await _firestoreService.GetUserProfileAsync(userId);
        App.CurrentUserProfile = _currentUserProfile;
        // Om _currentUserProfile är null kan du eventuellt visa ett meddelande eller skapa en ny profil
        if (_currentUserProfile == null)
        {
            await DisplayAlert("Fel", "Kunde inte ladda din profil.", "OK");
        }

        // Uppdatera UI med t.ex. befintligt Nickname och PlayerColor
        NicknameEntry.Text = _currentUserProfile.Nickname;

        // Om användaren redan har en sparad färg, sätt den som SelectedItem i CollectionView
        if (_currentUserProfile != null && !string.IsNullOrEmpty(_currentUserProfile.PlayerColor))
        {
            _selectedColor = _currentUserProfile.PlayerColor;
            ColorPickerCollectionView.SelectedItem = _selectedColor;
        }
    }
    private async void LoadUserProfile()
    {
        if (!string.IsNullOrEmpty(_userId))
        {
            var nickname = await _firestoreService.GetUserNicknameAsync(_userId);
            NicknameEntry.Text = nickname;
        }
    }

    private async void OnSaveNicknameClicked(object sender, EventArgs e)
    {
        var newNickname = NicknameEntry.Text.Trim();
        if (string.IsNullOrEmpty(newNickname))
        {
            await DisplayAlert("Fel", "Du måste ange ett nickname!", "OK");
            return;
        }

        await _firestoreService.UpdateUserNicknameAsync(_userId, newNickname);

        // Uppdatera globalt lagrat nickname
        App.LoggedInNickname = newNickname;
        Preferences.Set("Nickname", newNickname);

        //// Bekräfta ändring
        //SavedMessage.Text = "Ditt nickname har uppdaterats!";
        //SavedMessage.IsVisible = true;
    }
    private async void OnColorButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            // Hämta färgen från knappens bakgrund
            string selectedColor = btn.BackgroundColor.ToHex();
            _selectedColor = selectedColor;

            // Ta bort highlight från tidigare vald knapp
            if (_lastSelectedButton != null)
            {
                _lastSelectedButton.BorderColor = Colors.Transparent;
                _lastSelectedButton.BorderWidth = 0;
            }

            // Sätt en highlight på den aktuella knappen
            btn.BorderColor = Colors.Aqua; // eller välj någon annan highlight-färg
            btn.BorderWidth = 3;
            _lastSelectedButton = btn;
            // ColorPickerCollectionView.SelectedItem = _selectedColor;
        }
    }

    private async void OnSaveColorClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedColor))
        {
            await DisplayAlert("Error", "Ingen färg har valts.", "OK");
            return;
        }

        // Uppdatera den lokala modellen
        _currentUserProfile.PlayerColor = _selectedColor;
        // Anropa din FirestoreService för att uppdatera färgen i databasen
        await _firestoreService.UpdatePlayerColorAsync(_currentUserProfile.UserId, _selectedColor);

        await DisplayAlert("Sparat", "Din standardfärg har uppdaterats.", "OK");
    }

}