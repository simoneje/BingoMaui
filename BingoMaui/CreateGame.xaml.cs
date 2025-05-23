using Google.Cloud.Firestore;
using BingoMaui.Services;
using System;
using System.Threading.Tasks;
using BingoMaui.Services.Backend.RequestModels;
using static Google.Rpc.Context.AttributeContext.Types;
using BingoMaui.Services.Backend;
namespace BingoMaui;

public partial class CreateGame : ContentPage
{


    private List<Dictionary<string, object>> userChallenges = new();
    public CreateGame()
    {
        InitializeComponent();

        
    }
    private BingoCard ConvertDictionaryToBingoCard(Dictionary<string, object> dict)
    {
        return new BingoCard
        {
            Title = dict.ContainsKey("Title") ? dict["Title"].ToString() : string.Empty,
            Description = dict.ContainsKey("Description") ? dict["Description"].ToString() : string.Empty,
            Category = dict.ContainsKey("Category") ? dict["Category"].ToString() : string.Empty,
            CardId = dict.ContainsKey("CardId") ? dict["CardId"].ToString() : Guid.NewGuid().ToString() // Skapar ID om det inte finns
        };
    }
    private List<Challenge> ConvertBingoCardsToChallenges(List<BingoCard> bingoCards)
    {
        return bingoCards.Select(card => new Challenge
        {
            Title = card.Title,
            Description = card.Description,
            Category = card.Category,
            CompletedBy = new List<CompletedInfo>(), // Initiera en tom lista
            ChallengeId = card.CardId,
        }).ToList();
    }

    private async void OnCreateGameClicked(object sender, EventArgs e)
    {
        var combinedChallenges = await GetCombinedChallengesAsync();

        var challenges = ConvertBingoCardsToChallenges(combinedChallenges);
        var startDateUtc = StartDatePicker.Date.ToUniversalTime();
        var endDateUtc = EndDatePicker.Date.ToUniversalTime();

        var userProfile = App.CurrentUserProfile;
        string userColor = userProfile?.PlayerColor ?? "#FF5733";

        var gameRequest = new CreateGameRequest
        {
            GameName = GameNameEntry.Text,
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            Cards = combinedChallenges,
            Nickname = userProfile.Nickname,
            PlayerColor = userColor
        };

        var game = await BackendServices.GameService.CreateGameAsync(gameRequest);
        AccountServices.SaveGameToCache(game);

        if (game == null)
        {
            await DisplayAlert("Fel", "Kunde inte skapa spelet. Försök igen.", "OK");
            return;
        }

        App.ShouldRefreshChallenges = true;

        await Navigation.PushAsync(new BingoBricka(game.GameId));

        await DisplayAlert("Framgång!", $"Spelet {game.GameName} har skapats med Invite Code: {game.InviteCode}", "OK");
    }

    private string GenerateInviteCode()
    {
        return Guid.NewGuid().ToString().Substring(0, 6).ToUpper(); // Exempel: "ABC123"
    }
    private async void OnUpdateGameStatusClicked(object sender, EventArgs e)
    {
        
    }
    private async void OnDeleteGameClicked(object sender, EventArgs e)
    {
        
    }
    private async Task<List<BingoCard>> GetCombinedChallengesAsync()
    {
        int userChallengeCount = userChallenges.Count;
        List<BingoCard> bingoCards = new();

        if (userChallengeCount >= 25)
        {
            bingoCards = userChallenges
                .Take(25)
                .Select(dict => ConvertDictionaryToBingoCard(dict))
                .ToList();
        }
        else
        {
            int neededCount = 25 - userChallengeCount;
            var backendChallengeService = BackendServices.ChallengeService;

            var firebaseChallenges = await backendChallengeService.GetRandomChallengesAsync(neededCount);

            var userCards = userChallenges.Select(dict => ConvertDictionaryToBingoCard(dict));
            var firebaseCards = firebaseChallenges.Select(ch => Converters.ConvertChallengeToBingoCard(ch));

            bingoCards = userCards.Concat(firebaseCards).ToList();
        }
        return bingoCards;
    }
}