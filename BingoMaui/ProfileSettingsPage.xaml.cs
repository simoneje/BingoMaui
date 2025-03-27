using BingoMaui.Services;
namespace BingoMaui;
public partial class ProfileSettingsPage : ContentPage
{
    private readonly FirestoreService _firestoreService;
    private string _userId;

    public ProfileSettingsPage()
    {
        InitializeComponent();
        _firestoreService = new FirestoreService();
        _userId = Preferences.Get("UserId", string.Empty);

        LoadUserProfile();
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
}