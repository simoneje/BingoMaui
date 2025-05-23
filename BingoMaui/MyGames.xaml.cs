using BingoMaui.Services;
using BingoMaui.Services.Backend;
namespace BingoMaui;

public partial class MyGames : ContentPage
{
    private readonly BackendGameService _gameService;

    private string _userId;
    

    public MyGames()
    {
        InitializeComponent();
        _gameService = BackendServices.GameService;
        _userId = Preferences.Get("UserId", string.Empty); // H�mtar inloggad anv�ndares ID
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMyGamesAsync();
    }

    private async Task LoadMyGamesAsync()
    {
        // H�mta alla spel d�r anv�ndaren �r med

        var games = await _gameService.GetGamesForUserAsync();

        GamesList.ItemsSource = games;
        
    }
    private async void OnOpenGameClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var gameId = button.CommandParameter.ToString();
        var selectedGame = button.BindingContext as BingoGame;
        var game = AccountServices.LoadGameFromCache(gameId);

        if (game == null)
        {
            // H�mta fr�n backend och cacha
            game = await BackendServices.GameService.GetGameByIdAsync(gameId);
            if (game != null)
                AccountServices.SaveGameToCache(game);
        }


        if (game == null || game.Cards == null || game.Cards.Count == 0)
        {
            await DisplayAlert("Fel", "Inga bingobrickor hittades f�r detta spel.", "OK");
            return;
        }

        // Konvertera BingoCards till Challenges
        var challenges = Converters.ConvertBingoCardsToChallenges(game.Cards);

        App.ShouldRefreshChallenges = true;
        // Navigera till BingoBricka med gameId och konverterade utmaningar
        await Navigation.PushAsync(new BingoBricka(gameId));
    }
}