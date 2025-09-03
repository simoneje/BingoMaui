using BingoMaui.Services;
using BingoMaui.Services.Backend.RequestModels;
using System.Collections.ObjectModel;

namespace BingoMaui;

public partial class CreateGame : ContentPage
{
    // Egna utmaningar du lägger till i denna vy innan skapande
    private readonly ObservableCollection<BingoCard> _customChallenges = new();

    // Egna “userChallenges” om du har dem sedan tidigare (dictionary-shape)
    private List<Dictionary<string, object>> userChallenges = new();

    // Vald brädstorlek (ändras via Picker). Default 5x5.
    private int _boardSide = 5;
    private int DesiredCount => _boardSide * _boardSide;

    public CreateGame()
    {
        InitializeComponent();

        // Sätt default 5x5 på pickern om inget valt
        if (BoardSizePicker != null && BoardSizePicker.SelectedIndex < 0)
            BoardSizePicker.SelectedIndex = 0; // 5 x 5

        UpdateBoardSizeInfo();
        UpdateCustomCountLabel();
    }

    // === Picker: uppdatera vald storlek ===
    private void OnBoardSizeChanged(object sender, EventArgs e)
    {
        if (BoardSizePicker?.SelectedItem is string text)
        {
            // "7 x 7" → 7
            var digits = new string(text.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var n) && n >= 5 && n <= 10)
            {
                _boardSide = n;
                UpdateBoardSizeInfo();
                UpdateCustomCountLabel();
            }
        }
    }

    private void UpdateBoardSizeInfo()
    {
        if (BoardSizeInfoLabel != null)
            BoardSizeInfoLabel.Text = $"{DesiredCount} rutor";
    }

    private void UpdateCustomCountLabel()
    {
        if (ChallengeCountLabel != null)
            ChallengeCountLabel.Text = $"{_customChallenges.Count}/{DesiredCount} egna utmaningar";
    }

    // === Helpers: konverteringar ===
    private BingoCard ConvertDictionaryToBingoCard(Dictionary<string, object> dict)
    {
        return new BingoCard
        {
            Title = dict.TryGetValue("Title", out var t) ? t?.ToString() : string.Empty,
            Description = dict.TryGetValue("Description", out var d) ? d?.ToString() : string.Empty,
            Category = dict.TryGetValue("Category", out var c) ? c?.ToString() : string.Empty,
            // CardId kan vara tomt – backend är tolerant och genererar, men vi sätter gärna ett lokalt också
            CardId = dict.TryGetValue("CardId", out var id) && id != null ? id.ToString() : Guid.NewGuid().ToString()
        };
    }

    private List<Challenge> ConvertBingoCardsToChallenges(List<BingoCard> bingoCards)
    {
        return bingoCards.Select(card => new Challenge
        {
            Title = card.Title,
            Description = card.Description,
            Category = card.Category,
            CompletedBy = new List<CompletedInfo>(),
            ChallengeId = card.CardId,
        }).ToList();
    }

    // === Skapa spel ===
    private async void OnCreateGameClicked(object sender, EventArgs e)
    {
        var combinedChallenges = await GetCombinedChallengesAsync(DesiredCount);

        // Datum (backend defaultar också, men vi skickar med här)
        var startDateUtc = DateTime.UtcNow;
        var endDateUtc = startDateUtc.AddMonths(3);

        var userProfile = App.CurrentUserProfile;

        var gameRequest = new CreateGameRequest
        {
            GameName = string.IsNullOrWhiteSpace(GameNameEntry?.Text) ? null : GameNameEntry.Text,
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            Cards = combinedChallenges, // N*N kort
            Nickname = userProfile?.Nickname,
            PlayerColor = string.IsNullOrWhiteSpace(userProfile?.PlayerColor) ? "#00FFFF" : userProfile.PlayerColor
        };

        var game = await BackendServices.GameService.CreateGameAsync(gameRequest);

        if (game == null)
        {
            await DisplayAlert("Fel", "Kunde inte skapa spelet. Försök igen.", "OK");
            return;
        }

        AccountServices.SaveGameToCache(game);
        App.ShouldRefreshChallenges = true;

        await Navigation.PushAsync(new BingoBricka(game.GameId));
        await DisplayAlert("Framgång!", $"Spelet {game.GameName} har skapats med Invite Code: {game.InviteCode}", "OK");
    }

    // === (tom knapp-handler om du planerar något senare) ===
    private async void OnUpdateGameStatusClicked(object sender, EventArgs e)
    {
        await Task.CompletedTask;
    }

    // === Lägg till egen utmaning i denna session ===
    private async void OnAddCustomChallengeClicked(object sender, EventArgs e)
    {
        if (_customChallenges.Count >= DesiredCount)
        {
            await DisplayAlert("Max antal", $"Du kan max ha {DesiredCount} rutor för vald storlek.", "OK");
            return;
        }

        // 1) Titel (obligatorisk)
        var title = await DisplayPromptAsync("Ny utmaning", "Titel (obligatorisk):", "Lägg till", "Avbryt");
        if (string.IsNullOrWhiteSpace(title)) return;
        title = title.Trim();

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

        // Dubblettvarning på titel (case-insensitiv)
        if (_customChallenges.Any(c => string.Equals(c.Title, title, StringComparison.OrdinalIgnoreCase)))
        {
            var ok = await DisplayAlert("Dubblett", "En utmaning med samma titel finns redan. Lägga ändå?", "Ja", "Nej");
            if (!ok) return;
        }

        _customChallenges.Add(new BingoCard
        {
            CardId = Guid.NewGuid().ToString(), // lokalt id (backend genererar också om saknas)
            Title = title,
            Description = description,
            Category = category,
            CompletedBy = new List<CompletedInfo>()
        });

        UpdateCustomCountLabel();
    }

    // === Bygg upp till desiredCount: egna → userChallenges → backend → klona ===
    private async Task<List<BingoCard>> GetCombinedChallengesAsync(int desiredCount)
    {
        var result = new List<BingoCard>();

        // 1) Egna (lagda via knappen)
        if (_customChallenges.Count > 0)
            result.AddRange(_customChallenges);

        // 2) Egna gamla (dictionary) om de finns
        var userCards = userChallenges?
            .Select(dict => ConvertDictionaryToBingoCard(dict))
            .ToList() ?? new List<BingoCard>();

        if (userCards.Count > 0)
            result.AddRange(userCards);

        // 3) Fyll på från backend slump
        if (result.Count < desiredCount)
        {
            var needed = desiredCount - result.Count;
            var firebaseChallenges = await BackendServices.ChallengeService.GetRandomChallengesAsync(needed);
            var firebaseCards = firebaseChallenges.Select(ch => Converters.ConvertChallengeToBingoCard(ch));
            result.AddRange(firebaseCards);
        }

        // 4) Klona om vi fortfarande saknar
        if (result.Count == 0)
            throw new InvalidOperationException("Saknar helt underlag för att skapa rutor.");

        var basePool = result.ToList();
        int idx = 0;
        while (result.Count < desiredCount)
        {
            var src = basePool[idx % basePool.Count];
            result.Add(CloneForBoard(src));
            idx++;
        }

        // 5) Initiera CompletedBy och säkerställ unikt CardId
        foreach (var c in result)
        {
            c.CompletedBy = new List<CompletedInfo>();
            if (string.IsNullOrWhiteSpace(c.CardId))
                c.CardId = Guid.NewGuid().ToString();
        }

        return result;
    }

    // Klon med nytt CardId (separat ruta på brädet)
    private static BingoCard CloneForBoard(BingoCard src) => new BingoCard
    {
        CardId = Guid.NewGuid().ToString(),
        Title = src.Title,
        Description = src.Description,
        Category = src.Category,
        CompletedBy = new List<CompletedInfo>()
    };
}
