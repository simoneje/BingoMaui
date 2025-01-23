using Firebase;
using BingoMaui.Services;
namespace BingoMaui;

public partial class BingoBricka : ContentPage
{

    private readonly FirestoreService _firestoreService;
    private string _gameId;
    private List<Challenge> _challenges;
    public BingoBricka(string gameId, List<Challenge> challenges)
	{
		InitializeComponent();
        _firestoreService = new FirestoreService(); // Skapa en instans av tjänstklassen
        _gameId = gameId; // ID för specifika spelet som visas
        _challenges = challenges;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PopulateBingoGrid(_challenges);
    }
    private async Task LoadBingoCardAsync()
    {
        // 1. Hämta spelet från Firestore
        var game = await _firestoreService.GetGameByIdAsync(_gameId);
        if (game == null)
        {
            Console.WriteLine($"Game with ID {_gameId} not found.");
            await DisplayAlert("Error", "Spelet kunde inte laddas.", "OK");
            return;
        }

        // 2. Kontrollera om spelet har några "Cards"
        if (game.Cards == null || game.Cards.Count == 0)
        {
            Console.WriteLine("No cards found for the game.");
            await DisplayAlert("Error", "Inga bingobrickor hittades för spelet.", "OK");
            return;
        }

        // 3. Hämta detaljerna för utmaningarna baserat på Cards
        var challenges = await _firestoreService.GetChallengesForGameAsync(_gameId);

        // 4. Om inga utmaningar hittas, visa ett fel
        if (challenges == null || challenges.Count == 0)
        {
            Console.WriteLine("No challenges found for the game.");
            await DisplayAlert("Error", "Inga utmaningar hittades för spelet.", "OK");
            return;
        }

        // 5. Fyll bingobrickan med detaljerade utmaningar
        PopulateBingoGrid(challenges);

        Console.WriteLine($"Loaded game: {game.GameName}, Challenges: {challenges.Count}");
    }
    //private void PopulateBingoGrid(List<BingoCard> bingoCards)
    //{
    //    BingoGrid.Children.Clear();

    //    int index = 0;
    //    for (int row = 0; row < 5; row++)
    //    {
    //        for (int col = 0; col < 5; col++)
    //        {
    //            var challenge = bingoCards[index];
    //            var button = new Button
    //            {
    //                Text = challenge.Title,
    //                FontSize = CalculateFontSize(challenge.Title),
    //                HorizontalOptions = LayoutOptions.FillAndExpand,
    //                VerticalOptions = LayoutOptions.FillAndExpand,
    //                BackgroundColor = Colors.Purple,
    //                TextColor = Colors.White,
    //                Padding = new Thickness(2) // Minskat padding för att få mer utrymme
    //            };

    //            BingoGrid.Add(button, col, row);
    //            index++;
    //        }
    //    }
    //}

    private async void PopulateBingoGrid(List<Challenge> challenges)
    {
        BingoGrid.Children.Clear();
        int index = 0;

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                if (index >= challenges.Count)
                    break;
                var challenge = challenges[index];

                // Skapa knappen
                var button = new Button
                {
                    Text = challenge.Title,
                    FontSize = CalculateFontSize(challenge.Title),
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    BackgroundColor = Colors.Purple,
                    TextColor = Colors.White,
                    Padding = new Thickness(5),
                    TextTransform = TextTransform.None
                };
                button.LineBreakMode = LineBreakMode.WordWrap;
                // Klick-händelse: Navigera till ChallengeDetails
                button.Clicked += async (sender, args) =>
                {
                    var challengeDetailsPage = new ChallengeDetails(_gameId, challenge);
                    await Navigation.PushAsync(challengeDetailsPage);
                };

                // Lägg till knappen i grid
                BingoGrid.Add(button, col, row);
                index++;
            }
        }
    }
    
    private async Task LoadBingoGridAsync(string gameId)
    {
        // Hämta utmaningarna från Firebase
        var challenges = await _firestoreService.GetChallengesForGameAsync(gameId);

        // Fyll bingobrickan med utmaningarna
        PopulateBingoGrid(challenges);
    }
    private double CalculateFontSize(string text)
    {
        if (text.Length > 30)
            return 8; // Väldigt lång text
        else if (text.Length > 20)
            return 10; // Medellång text
        else
            return 12; // Kort text
    }
}
