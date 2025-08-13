using BingoMaui.Services;
using System;
using System.Threading.Tasks;

namespace BingoMaui;

public partial class JoinGame : ContentPage
{

    private readonly string selectedColor = "";

    public JoinGame()
    {
        InitializeComponent();

    }

    private async void OnJoinGameClicked(object sender, EventArgs e)
    {
        try
        {
            var inviteCode = InviteCodeEntry.Text?.ToUpper().Trim();
            if (string.IsNullOrEmpty(inviteCode))
            {
                await DisplayAlert("Fel", "Skriv in en giltig invite-kod.", "OK");
                return;
            }

            var game = await BackendServices.GameService.JoinGameAsync(
                inviteCode,
                App.CurrentUserProfile.Nickname,
                App.CurrentUserProfile.PlayerColor
            );
            

            if (game == null)
            {
                await DisplayAlert("Fel", "Kunde inte gå med i spelet. Koden kan vara ogiltig eller du är redan med.", "OK");
                return;
            }

            var challenges = Converters.ConvertBingoCardsToChallenges(game.Cards);
            await DisplayAlert("Success", $"Du har gått med i spelet: {game.GameName}!", "OK");
            await Navigation.PushAsync(new BingoBricka(game.GameId));
            App.ShouldRefreshChallenges = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔥 Error in OnJoinGameClicked: {ex.Message}");
            await DisplayAlert("Fel", "Ett oväntat fel inträffade.", "OK");
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