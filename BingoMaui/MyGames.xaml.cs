using BingoMaui.Services;
namespace BingoMaui;

public partial class MyGames : ContentPage
{
    private readonly FirestoreService _firestoreService;
    private string _userId;
    

    public MyGames()
    {
        InitializeComponent();
        _firestoreService = new FirestoreService();
        _userId = Preferences.Get("UserId", string.Empty); // Hämtar inloggad användares ID
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMyGamesAsync();
    }

    private async Task LoadMyGamesAsync()
    {
        // Hämta alla spel där användaren är med

        var games = await _firestoreService.GetGamesForUserAsync(_userId);
        GamesList.ItemsSource = games;
        
    }
    private async void OnOpenGameClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var gameId = button.CommandParameter.ToString();
        var selectedGame = button.BindingContext as BingoGame;
        // Hämta spelet från Firestore
        var game = await _firestoreService.GetGameByIdAsync(gameId);

        if (game == null || game.Cards == null || game.Cards.Count == 0)
        {
            await DisplayAlert("Fel", "Inga bingobrickor hittades för detta spel.", "OK");
            return;
        }
        
        // Konvertera BingoCards till Challenges
        var challenges = _firestoreService.ConvertBingoCardsToChallenges(game.Cards);
        App.ShouldRefreshChallenges = true;
        // Navigera till BingoBricka med gameId och konverterade utmaningar
        await Navigation.PushAsync(new BingoBricka(gameId, challenges));
    }
}