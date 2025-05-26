using BingoMaui.Services;
using BingoMaui.Services.Backend;
using Firebase.Auth;
using static System.Net.Mime.MediaTypeNames;
using System;
namespace BingoMaui;

public partial class StartPage : ContentPage
{
    private readonly FirestoreService _firestoreService;
    public StartPage()
    {
        InitializeComponent();
        _firestoreService = new FirestoreService();

    }
    protected override async void OnAppearing()
    {
        string userId = Preferences.Get("UserId", string.Empty);
        if (App.CurrentUserProfile == null)
            App.CurrentUserProfile = new UserProfile();
        App.CurrentUserProfile = await BackendServices.MiscService.GetUserProfileFromApiAsync();
        // Använd global variabel för att visa nickname
        WelcomeLabel.Text = $"Välkommen, {App.CurrentUserProfile.Nickname}!";
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
    private async void OnNavigateButtonClickedProfile(object sender, EventArgs e)
    {
        string userId = Preferences.Get("UserId", string.Empty);
        await Navigation.PushAsync(new ProfilePublicPage(userId));
    }
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Logga ut", "Vill du logga ut?", "Ja", "Avbryt");
        if (confirm)
        {
            await AccountServices.LogoutAsync();
        }
    }
    private async void OnMyGamesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MyGames());
    }

}