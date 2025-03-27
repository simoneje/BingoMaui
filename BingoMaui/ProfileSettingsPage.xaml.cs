using BingoMaui.Services;
namespace BingoMaui;
public partial class ProfileSettingsPage : ContentPage
{
    private readonly FirestoreService _firestoreService;
    private string _userId;
    private UserProfile _currentUserProfile;
    private string _selectedColor = string.Empty; // H�ller den valda f�rgen
    private Button _lastSelectedButton = null;      // H�ller referensen till den tidigare klickade knappen
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
            await DisplayAlert("Fel", "Inget anv�ndar-ID hittades.", "OK");
            return;
        }

        // H�mta anv�ndarprofilen fr�n Firestore (skapa/ut�ka en metod GetUserProfileAsync i FirestoreService)
        _currentUserProfile = await _firestoreService.GetUserProfileAsync(userId);
        App.CurrentUserProfile = _currentUserProfile;
        // Om _currentUserProfile �r null kan du eventuellt visa ett meddelande eller skapa en ny profil
        if (_currentUserProfile == null)
        {
            await DisplayAlert("Fel", "Kunde inte ladda din profil.", "OK");
        }

        // Uppdatera UI med t.ex. befintligt Nickname och PlayerColor
        NicknameEntry.Text = _currentUserProfile.Nickname;

        // Om anv�ndaren redan har en sparad f�rg, s�tt den som SelectedItem i CollectionView
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
            await DisplayAlert("Fel", "Du m�ste ange ett nickname!", "OK");
            return;
        }

        await _firestoreService.UpdateUserNicknameAsync(_userId, newNickname);

        // Uppdatera globalt lagrat nickname
        App.LoggedInNickname = newNickname;
        Preferences.Set("Nickname", newNickname);

        //// Bekr�fta �ndring
        //SavedMessage.Text = "Ditt nickname har uppdaterats!";
        //SavedMessage.IsVisible = true;
    }
    private async void OnColorButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            // H�mta f�rgen fr�n knappens bakgrund
            string selectedColor = btn.BackgroundColor.ToHex();
            _selectedColor = selectedColor;

            // Ta bort highlight fr�n tidigare vald knapp
            if (_lastSelectedButton != null)
            {
                _lastSelectedButton.BorderColor = Colors.Transparent;
                _lastSelectedButton.BorderWidth = 0;
            }

            // S�tt en highlight p� den aktuella knappen
            btn.BorderColor = Colors.Aqua; // eller v�lj n�gon annan highlight-f�rg
            btn.BorderWidth = 3;
            _lastSelectedButton = btn;
            // ColorPickerCollectionView.SelectedItem = _selectedColor;
        }
    }

    private async void OnSaveColorClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedColor))
        {
            await DisplayAlert("Error", "Ingen f�rg har valts.", "OK");
            return;
        }

        // Uppdatera den lokala modellen
        _currentUserProfile.PlayerColor = _selectedColor;
        // Anropa din FirestoreService f�r att uppdatera f�rgen i databasen
        await _firestoreService.UpdatePlayerColorAsync(_currentUserProfile.UserId, _selectedColor);

        await DisplayAlert("Sparat", "Din standardf�rg har uppdaterats.", "OK");
    }

}