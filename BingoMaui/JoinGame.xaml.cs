using BingoMaui.Services;
using System;
using System.Threading.Tasks;

namespace BingoMaui;

public partial class JoinGame : ContentPage
{
    private readonly FirestoreService _firestoreService;

    public JoinGame()
    {
        InitializeComponent();
        _firestoreService = new FirestoreService(); // Skapa en instans av FirestoreService
    }

    private async void OnJoinGameClicked(object sender, EventArgs e)
    {
        var inviteCode = InviteCodeEntry.Text;

        if (string.IsNullOrEmpty(inviteCode))
        {
            await DisplayAlert("Error", "Du m�ste ange en invite-kod!", "OK");
            return;
        }

        // 1. H�mta spelet baserat p� invite-koden
        var game = await _firestoreService.GetGameByInviteCodeAsync(inviteCode);

        if (game != null)
        {
            var userId = Preferences.Get("UserId", string.Empty); // H�mta inloggad anv�ndares ID
            if (game.Players.Contains(userId))
            {
                await DisplayAlert("Info", "Du �r redan med i detta spel!", "OK");
                return;
            }

            // 2. L�gg till spelaren i spelets lista
            await _firestoreService.AddPlayerToGameAsync(game.DocumentId, userId, game.GameName);

            // 3. H�mta utmaningarna f�r spelet
            var challenges = await _firestoreService.GetChallengesForGameAsync(game.GameId);

            if (challenges == null || challenges.Count == 0)
            {
                await DisplayAlert("Error", "Inga utmaningar hittades f�r spelet.", "OK");
                return;
            }

            // 4. Navigera till BingoBricka med utmaningarna
            await DisplayAlert("Success", $"Du har g�tt med i spelet: {game.GameName}!", "OK");
            await Navigation.PushAsync(new BingoBricka(game.GameId, challenges));
        }
        else
        {
            await DisplayAlert("Error", "Spelet med den koden hittades inte!", "OK");
        }
    }
    private List<Challenge> ConvertBingoCardsToChallenges(List<BingoCard> bingoCards)
    {
        return bingoCards.Select(card => new Challenge
        {
            ChallengeId = card.CardId,
            Title = card.Title,
            Description = card.Description,
            Category = card.Category,
            CompletedBy = new List<string>() // Initiera en tom lista f�r completedBy
        }).ToList();
    }
}
