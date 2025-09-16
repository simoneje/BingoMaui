
using BingoMaui.Services;
using System.Text.Json;

namespace BingoMaui;

public partial class BingoBricka : ContentPage
{
    private string _gameId;
    private List<Challenge> _challenges = new();
    private string _inviteCode = string.Empty;
    private string _currentUserId;


    // ---- Layout/scroll parametrar ----
    private const int BaseVisibleCols = 5;   // “så här många kolumner får plats utan scroll” (behåll nuvarande look)
    private const double TileSpacing = 5;    // matchar XAML Grid.Row/ColumnSpacing
    private const double GridSidePadding = 20; // BingoGrid.Padding left+right (10 + 10)

    private double _tileSize = 0;            // beräknas utifrån skärmbredd och BaseVisibleCols
    private bool _layoutReady = false;       // sätts när vi har mått för BoardScroll
    public BingoBricka(string gameId)
    {
        InitializeComponent();
        _gameId = gameId;

        // Lyssna när layouten fått storlek för att räkna fram tegelstorlek
        SizeChanged += OnPageSizeChanged;
    }
    // Beräkna tile-storlek när vi fått faktiska mått
    private void OnPageSizeChanged(object sender, EventArgs e)
    {
        if (BoardScroll?.Width > 0)
        {
            var width = BoardScroll.Width; // tillgänglig vybredd för brädet
            // total horisontell spacing mellan 5 kolumner = 4 * TileSpacing
            var totalSpacing = TileSpacing * (BaseVisibleCols - 1);
            var usable = width - GridSidePadding - totalSpacing;
            var size = Math.Floor(usable / BaseVisibleCols);

            // Rimlig min/max-säkring
            if (size < 60) size = 60;
            if (size > 220) size = 220;

            if (Math.Abs(_tileSize - size) > 0.1)
            {
                _tileSize = size;
                _layoutReady = true;

                // Om vi redan har data – rendera om med ny storlek
                if (_challenges.Count > 0)
                    PopulateBingoGrid(_challenges);
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Hämta aktuell användare (lokalt från SecureStorage via din helper)
        _currentUserId = await BackendServices.GetUserIdAsync();

        // 1) Rendera snabbt från cache
        var cachedGame = AccountServices.LoadGameFromCache(_gameId);
        if (cachedGame != null)
        {
            _inviteCode = cachedGame.InviteCode;
            _challenges = Converters.ConvertBingoCardsToChallenges(cachedGame.Cards);
            InviteCodeLabel.Text = _inviteCode;
            if (_layoutReady) PopulateBingoGrid(_challenges);
        }

        // 2) Hämta färskt från backend
        try
        {
            var latestGame = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            if (latestGame != null)
            {
                bool shouldRefresh =
                    cachedGame == null ||
                    cachedGame.Cards?.Count != latestGame.Cards?.Count ||
                    !cachedGame.Cards.Select(c => c.CardId)
                                     .SequenceEqual(latestGame.Cards.Select(c => c.CardId));

                if (shouldRefresh)
                {
                    _challenges = Converters.ConvertBingoCardsToChallenges(latestGame.Cards);
                    if (_layoutReady) PopulateBingoGrid(_challenges);
                }

                _inviteCode = latestGame.InviteCode;
                InviteCodeLabel.Text = _inviteCode;

                // 3) Uppdatera cache
                AccountServices.SaveGameToCache(latestGame);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔌 Fel vid hämtning från backend: {ex.Message}");
        }
    }



    private async void PopulateBingoGrid(List<Challenge> challenges)
    {
        try
        {
            if (BingoGrid == null || challenges == null || challenges.Count == 0)
            {
                Console.WriteLine("BingoGrid är null eller inga utmaningar.");
                await DisplayAlert("Fel", "Inga utmaningar att visa.", "OK");
                return;
            }

            if (_tileSize <= 0)
            {
                // Har vi ingen storlek ännu? Försök trigga om senare
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // När SizeChanged kör, kommer den rendera om.
                });
                return;
            }

            BingoGrid.Children.Clear();
            BingoGrid.RowDefinitions.Clear();
            BingoGrid.ColumnDefinitions.Clear();

            int totalItems = challenges.Count;
            int gridSize = (int)Math.Ceiling(Math.Sqrt(totalItems)); // NxN

            // Auto-storlek på rader/kolumner (barnen sätter Width/HeightRequest)
            for (int r = 0; r < gridSize; r++)
                BingoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < gridSize; c++)
                BingoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            for (int i = 0; i < totalItems; i++)
            {
                var challenge = challenges[i];
                int row = i / gridSize;
                int col = i % gridSize;

                // En fast storleks-container per ruta
                var tileContainer = new ContentView
                {
                    WidthRequest = _tileSize,
                    HeightRequest = _tileSize,
                    Padding = 0
                };

                // Inre grid i varje ruta
                var tileGrid = new Grid
                {
                    BackgroundColor = Colors.Purple,
                    Padding = 6,
                    RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Star },
                        new RowDefinition { Height = GridLength.Auto },
                    }
                };

                // Titel
                var titleLabel = new Label
                {
                    Text = challenge.Title ?? "Okänd utmaning",
                    FontSize = CalculateFontSize(challenge.Title),
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.WordWrap,
                    HorizontalTextAlignment = TextAlignment.Center,
                    MaxLines = 4
                };
                tileGrid.Add(titleLabel, 0, 0);

                // Prickar eller +X
                if (challenge.CompletedBy != null && challenge.CompletedBy.Count > 0)
                {
                    var completions = challenge.CompletedBy;
                    var dotsGrid = new Grid
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        Padding = 0
                    };

                    var userId = !string.IsNullOrEmpty(_currentUserId) ? _currentUserId : App.CurrentUserProfile?.UserId;
                    var myCompletion = completions.FirstOrDefault(c => c.PlayerId == userId);
                    int totalCount = completions.Count;
                    bool isMine = myCompletion != null;

                    if (totalCount > 4)
                    {
                        int colDot = 0;

                        if (isMine)
                        {
                            dotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            var myDot = new BoxView
                            {
                                WidthRequest = 10,
                                HeightRequest = 10,
                                CornerRadius = 5,
                                BackgroundColor = Color.FromArgb(myCompletion.UserColor ?? "#FFFFFF"),
                                Margin = new Thickness(1)
                            };

                            dotsGrid.Add(myDot, colDot++, 0);
                        }

                        var remaining = isMine ? totalCount - 1 : totalCount;

                        dotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var badge = new Frame
                        {
                            BackgroundColor = Colors.Black.WithAlpha(0.6f),
                            CornerRadius = 6,
                            Padding = new Thickness(6, 2),
                            Content = new Label
                            {
                                Text = $"+{remaining}",
                                TextColor = Colors.White,
                                FontSize = 10,
                                HorizontalTextAlignment = TextAlignment.Center
                            },
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            HasShadow = false
                        };

                        var tap = new TapGestureRecognizer
                        {
                            Command = new Command(async () =>
                            {
                                await ShowAllCompletedPlayers(completions);
                            })
                        };
                        badge.GestureRecognizers.Add(tap);

                        dotsGrid.Add(badge, colDot, 0);
                    }
                    else
                    {
                        int col2 = 0;
                        foreach (var c in completions)
                        {
                            dotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                            var dot = new BoxView
                            {
                                WidthRequest = 10,
                                HeightRequest = 10,
                                CornerRadius = 5,
                                BackgroundColor = Color.FromArgb(c.UserColor ?? "#FFFFFF"),
                                Margin = new Thickness(1)
                            };

                            dotsGrid.Add(dot, col2++, 0);
                        }
                    }

                    tileGrid.Add(dotsGrid, 0, 1);
                }

                // Tap på hela rutan
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(_gameId))
                    {
                        var challengeDetailsPage = new ChallengeDetails(_gameId, challenge);
                        await Navigation.PushAsync(challengeDetailsPage);
                    }
                };
                tileGrid.GestureRecognizers.Add(tapGesture);

                tileContainer.Content = tileGrid;
                BingoGrid.Add(tileContainer, col, row);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel vid generering av bingobricka: {ex.Message}");
            await DisplayAlert("Fel", "Kunde inte bygga bingobrickan.", "OK");
        }
    }

    private async void OnGameSettingsClicked(object sender, EventArgs e)
    {
        try
        {
            var game = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            if (game == null)
            {
                await DisplayAlert("Fel", "Kunde inte hämta spelet.", "OK");
                return;
            }

            await Navigation.PushAsync(new GameSettings(_gameId, game.PlayerInfo));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel vid navigering till inställningar: {ex.Message}");
            await DisplayAlert("Fel", "Kunde inte öppna inställningar för spelet.", "OK");
        }
    }

    private double CalculateFontSize(string text)
    {
        if (string.IsNullOrEmpty(text)) return 12;
        if (text.Length > 30) return 8;
        if (text.Length > 20) return 10;
        return 12;
    }

    private async void OnShowLeaderboardClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new BingoMaui.Leaderboard(_gameId));
    }

    private async void OnToggleCommentsClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new CommentModal(_gameId));
    }

    private async Task ShowAllCompletedPlayers(List<CompletedInfo> completedList)
    {
        string playerNames = string.Join("\n", completedList.Select(c =>
            $"{c.Nickname} ({c.UserColor})"));

        await DisplayAlert("Spelare som klarat denna:", playerNames, "OK");
    }

}
