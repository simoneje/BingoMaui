using BingoMaui.Services;
using BingoMaui.Services.Backend;
using BingoMaui.Services.Backend.RequestModels;
using Google.Cloud.Firestore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using static Google.Rpc.Context.AttributeContext.Types;
namespace BingoMaui;

public partial class CreateGame : ContentPage
{
    // Tillfällig lista för egna utmaningar i denna session
    private readonly ObservableCollection<BingoCard> _customChallenges = new();
    private const int BoardSize = 25;
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
        

        var gameRequest = new CreateGameRequest
        {
            GameName = GameNameEntry.Text,
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            Cards = combinedChallenges,
            Nickname = userProfile.Nickname,
            PlayerColor = string.IsNullOrWhiteSpace(userProfile.PlayerColor) ? "#00FFFF" : userProfile.PlayerColor
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

    private async void OnUpdateGameStatusClicked(object sender, EventArgs e)
    {
        
    }
    private async void OnAddCustomChallengeClicked(object sender, EventArgs e)
    {
        if (_customChallenges.Count >= BoardSize)
        {
            await DisplayAlert("Max antal", $"Du kan max ha {BoardSize} rutor.", "OK");
            return;
        }

        // 1) Titel (obligatorisk)
        var title = await DisplayPromptAsync("Ny utmaning", "Titel (obligatorisk):", "Lägg till", "Avbryt");
        if (string.IsNullOrWhiteSpace(title)) return;
        title = title.Trim();

        // Enkla valideringar
        if (title.Length > 100)
        {
            await DisplayAlert("För långt", "Titeln bör vara max 100 tecken.", "OK");
            return;
        }

        // 2) Beskrivning (valfri)
        var description = await DisplayPromptAsync("Ny utmaning", "Beskrivning (valfri):", "OK", "Hoppa över", null, -1, Keyboard.Text, "");
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        // 3) Kategori (valfri)
        var category = await DisplayPromptAsync("Ny utmaning", "Kategori (valfri):", "OK", "Hoppa över");
        category = string.IsNullOrWhiteSpace(category) ? null : category.Trim();

        // Undvik dubbletter på titel (case-insensitivt)
        if (_customChallenges.Any(c => string.Equals(c.Title, title, StringComparison.OrdinalIgnoreCase)))
        {
            var ok = await DisplayAlert("Dubblett", "En utmaning med samma titel finns redan. Lägga ändå?", "Ja", "Nej");
            if (!ok) return;
        }

        _customChallenges.Add(new BingoCard
        {
            // CardId lämnas tomt – backend genererar
            Title = title,
            Description = description,
            Category = category,
            CompletedBy = new List<CompletedInfo>() // bra att initiera
        });

        // (Valfritt) uppdatera en label i UI med count
        ChallengeCountLabel.Text = $"{_customChallenges.Count}/{BoardSize} egna utmaningar";
    }
    private async Task<List<BingoCard>> GetCombinedChallengesAsync()
    {
        var result = new List<BingoCard>();

        // 1) Egna utmaningar först (max 25)
        var takeCustom = Math.Min(_customChallenges.Count, BoardSize);
        result.AddRange(_customChallenges.Take(takeCustom));

        // 2) Fyll upp med backend-random tills vi når 25
        var remaining = BoardSize - result.Count;
        if (remaining > 0)
        {
            var backendChallengeService = BackendServices.ChallengeService;
            var firebaseChallenges = await backendChallengeService.GetRandomChallengesAsync(remaining);

            // Konvertera backendutmaningar till BingoCard (din befintliga converter)
            var moreCards = firebaseChallenges.Select(ch => Converters.ConvertChallengeToBingoCard(ch));

            result.AddRange(moreCards);
        }

        // Säkerställ att CompletedBy aldrig är null (UI gillar det)
        foreach (var c in result)
            c.CompletedBy ??= new List<CompletedInfo>();

        return result;
    }

    //private async Task<List<BingoCard>> GetCombinedChallengesAsync()
    //{
    //    int userChallengeCount = userChallenges.Count;
    //    List<BingoCard> bingoCards = new();

    //    if (userChallengeCount >= 25)
    //    {
    //        bingoCards = userChallenges
    //            .Take(25)
    //            .Select(dict => ConvertDictionaryToBingoCard(dict))
    //            .ToList();
    //    }
    //    else
    //    {
    //        int neededCount = 25 - userChallengeCount;
    //        var backendChallengeService = BackendServices.ChallengeService;

    //        var firebaseChallenges = await backendChallengeService.GetRandomChallengesAsync(neededCount);

    //        var userCards = userChallenges.Select(dict => ConvertDictionaryToBingoCard(dict));
    //        var firebaseCards = firebaseChallenges.Select(ch => Converters.ConvertChallengeToBingoCard(ch));

    //        bingoCards = userCards.Concat(firebaseCards).ToList();
    //    }
    //    return bingoCards;
    //}
}