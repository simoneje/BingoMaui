using Google.Cloud.Firestore;
using BingoMaui.Services;
using System;
using System.Threading.Tasks;
namespace BingoMaui;

public partial class CreateGame : ContentPage
{

    private readonly FirestoreService _firestoreService;
    private List<Dictionary<string, object>> userChallenges = new();
    public CreateGame()
    {
        InitializeComponent();
        _firestoreService = new FirestoreService();
        
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

        // Konvertera BingoCards till Challenges
        var challenges = ConvertBingoCardsToChallenges(combinedChallenges);
        // Konvertera datumen till UTC
        var startDateUtc = StartDatePicker.Date.ToUniversalTime();
        var endDateUtc = EndDatePicker.Date.ToUniversalTime();

        // Hämta inloggad användares ID från Preferences
        var hostId = App.CurrentUserProfile.UserId;
        if (string.IsNullOrEmpty(hostId))
        {
            await DisplayAlert("Fel", "Användar-ID kunde inte hittas. Logga in igen.", "OK");
            return;
        }

        var userProfile = App.CurrentUserProfile;
        // Använd defaultfärgen från profilen, annars en fallback (t.ex. vit)
        string userColor = userProfile?.PlayerColor ?? "#FF5733";
        var bingoGame = new BingoGame
        {
            GameId = Guid.NewGuid().ToString(),
            GameName = GameNameEntry.Text,
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            Status = "Active",
            Cards = combinedChallenges, // Kombinerade BingoCards
            InviteCode = GenerateInviteCode(),
            PlayerInfo = new Dictionary<string, PlayerStats>
            {
                { hostId, new PlayerStats { Color = userColor, Points = 0, Nickname = App.CurrentUserProfile.Nickname } }
            },
            PlayerIds = new List<string> { hostId } // ✅ lägg till här
        };

        // Spara spelet i Firestore
        await _firestoreService.CreateBingoGameAsync(bingoGame);
        App.ShouldRefreshChallenges = true;
        // Navigera till BingoBricka och skicka med både GameId och Challenges
        await Navigation.PushAsync(new BingoBricka(bingoGame.GameId, challenges));

        // Bekräftelse och navigering tillbaka
        await DisplayAlert("Framgång!", $"Spelet {bingoGame.GameName} har skapats med Invite Code: {bingoGame.InviteCode}", "OK");
    }
    private string GenerateInviteCode()
    {
        return Guid.NewGuid().ToString().Substring(0, 6).ToUpper(); // Exempel: "ABC123"
    }
    private async void OnUpdateGameStatusClicked(object sender, EventArgs e)
    {
        string gameId = "someGameId"; // Här ska du dynamiskt välja rätt GameId
        string newStatus = "Finished";
        await _firestoreService.UpdateBingoGameStatusAsync(gameId, newStatus);
    }
    private async void OnDeleteGameClicked(object sender, EventArgs e)
    {
        string gameId = "someGameId"; // Här ska du dynamiskt välja rätt GameId
        await _firestoreService.DeleteBingoGameAsync(gameId);
    }
    private async Task<List<BingoCard>> GetCombinedChallengesAsync()
    {
        // Hämta antalet användarskapade utmaningar
        int userChallengeCount = userChallenges.Count;

        List<BingoCard> bingoCards = new();

        if (userChallengeCount >= 25)
        {
            // Konvertera användarskapade utmaningar direkt till BingoCards
            bingoCards = userChallenges.Take(25)
                                        .Select(ConvertDictionaryToBingoCard)
                                        .ToList();
        }
        else
        {
            // Hämta resterande utmaningar från Firebase
            int neededCount = 25 - userChallengeCount;
            var firebaseChallenges = await _firestoreService.GetRandomChallengesAsync(neededCount);

            // Kombinera användarskapade och Firebase-utmaningar
            bingoCards = userChallenges.Select(ConvertDictionaryToBingoCard)
                                       .Concat(firebaseChallenges.Select(ConvertDictionaryToBingoCard))
                                       .ToList();
        }

        return bingoCards;
    }


}