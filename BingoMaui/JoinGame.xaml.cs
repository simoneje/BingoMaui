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


            var res = await BackendServices.GameService.JoinGameAsyncDetailed(inviteCode, App.CurrentUserProfile.Nickname, App.CurrentUserProfile.PlayerColor);

            if (res == null || res.Game == null)
            {
                await DisplayAlert("Fel", "Kunde inte gå med i spelet.", "OK");
                return;
            }

            if (res.AlreadyInGame)
            {
                await DisplayAlert("Info", "Du är redan med i spelet. Vi öppnar det åt dig.", "OK");
            }
            else
            {
                await DisplayAlert("Klart!", "Du har gått med i spelet!", "OK");
            }


            await Navigation.PushAsync(new BingoBricka(res.Game.GameId));
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