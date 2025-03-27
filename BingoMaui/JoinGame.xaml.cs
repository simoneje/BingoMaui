using BingoMaui.Services;
using System;
using System.Threading.Tasks;

namespace BingoMaui;

public partial class JoinGame : ContentPage
{
    private readonly FirestoreService _firestoreService;
    private readonly string selectedColor = "";

    public JoinGame()
    {
        InitializeComponent();
        _firestoreService = new FirestoreService(); // Skapa en instans av FirestoreService
    }

    private async void OnJoinGameClicked(object sender, EventArgs e)
    {
        try
        {
            var inviteCode = InviteCodeEntry.Text;

            if (string.IsNullOrEmpty(inviteCode))
            {
                await DisplayAlert("Error", "Du måste ange en invite-kod!", "OK");
                return;
            }

            // 1. Hämta spelet baserat på invite-koden
            var game = await _firestoreService.GetGameByInviteCodeAsync(inviteCode);

            if (game != null)
            {
                var userId = Preferences.Get("UserId", string.Empty); // Hämta inloggad användares ID
                if (game.Players.Contains(userId))
                {
                    await DisplayAlert("Info", "Du är redan med i detta spel!", "OK");
                    return;
                }

                // 2. Hämta användarprofilen för att få deras färg
                var userProfile = await _firestoreService.GetUserProfileAsync(userId);
                App.CurrentUserProfile = userProfile;
                // Använd defaultfärgen från profilen, annars en fallback (t.ex. vit)
                string userColor = userProfile?.PlayerColor ?? "#FF5733";

                // 3. Lägg till spelaren i spelets lista med användarens färg
                await _firestoreService.AddPlayerToGameAsync(game.DocumentId, userId, game.GameName, userColor);

                // 4. Hämta utmaningarna för spelet
                var challenges = _firestoreService.ConvertBingoCardsToChallenges(game.Cards);
                if (challenges == null || challenges.Count == 0)
                {
                    await DisplayAlert("Error", "Inga utmaningar hittades för spelet.", "OK");
                    return;
                }

                // 5. Navigera till BingoBricka med utmaningarna
                await DisplayAlert("Success", $"Du har gått med i spelet: {game.GameName}!", "OK");
                await Navigation.PushAsync(new BingoBricka(game.GameId, challenges));
            }
            else
            {
                await DisplayAlert("Error", "Spelet med den koden hittades inte!", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnJoinGameClicked: {ex.Message}");
            await DisplayAlert("Error", "Ett fel inträffade när du försökte gå med i spelet. Försök igen senare.", "OK");
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
            CompletedBy = new List<CompletedInfo>() // Initiera en tom lista för completedBy
        }).ToList();
    }
}