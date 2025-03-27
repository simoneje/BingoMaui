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
        var hostId = Preferences.Get("UserId", string.Empty);
        if (string.IsNullOrEmpty(hostId))
        {
            await DisplayAlert("Fel", "Användar-ID kunde inte hittas. Logga in igen.", "OK");
            return;
        }

        var bingoGame = new BingoGame
        {
            GameId = Guid.NewGuid().ToString(),
            GameName = GameNameEntry.Text,
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            Status = "Active",
            Cards = combinedChallenges, // Kombinerade BingoCards
            Players = new List<string> { hostId }, // Lägg till HostId som första spelare
            InviteCode = GenerateInviteCode(),
            Leaderboard = new Dictionary<string, int> { { hostId, 0 } }
        };

        // Spara spelet i Firestore
        await _firestoreService.CreateBingoGameAsync(bingoGame);

        // Navigera till BingoBricka och skicka med både GameId och Challenges
        await Navigation.PushAsync(new BingoBricka(bingoGame.GameId, challenges));

        // Bekräftelse och navigering tillbaka
        await DisplayAlert("Framgång!", $"Spelet {bingoGame.GameName} har skapats med Invite Code: {bingoGame.InviteCode}", "OK");
        await Navigation.PopAsync();
    }

    //private async Task CreateBingoGameWithInviteCodeAsync(DateTime startDate, DateTime endDate, string GameName, string HostId)
    //{
    //    var bingoGame = new BingoGame
    //    {
    //        GameId = Guid.NewGuid().ToString(), // Unikt ID
    //        GameName = GameName,
    //        HostId = HostId,
    //        StartDate = startDate,
    //        EndDate = endDate,
    //        Status = "Active",
    //        Cards = new List<BingoCard>(),
    //        Players = new List<string> { HostId }, // Lägg till HostId direkt i spelarnas lista
    //        InviteCode = GenerateInviteCode() // Generera invite-koden
    //    };

    //    var firestoreService = new FirestoreService();
    //    await firestoreService.CreateBingoGameAsync(bingoGame);

    //    Console.WriteLine($"Bingo game created with invite code: {bingoGame.InviteCode}");
    //}
    private string GenerateInviteCode()
    {
        return Guid.NewGuid().ToString().Substring(0, 6).ToUpper(); // Exempel: "ABC123"
    }
    /*private async void OnGetGamesClicked(object sender, EventArgs e)
    {
        var games = await _firestoreService.GetBingoGamesAsync();

        foreach (var game in games)
        {
            Console.WriteLine($"Game Name: {game.GameName}, Status: {game.Status}");
        }
    }*/

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