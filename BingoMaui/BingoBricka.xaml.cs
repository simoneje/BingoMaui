
using BingoMaui.Services;
using System.Text.Json;
namespace BingoMaui;

public partial class BingoBricka : ContentPage
{
    private double _currentScale = 1;
    private double _startScale = 1;
    private double _xOffset = 0;
    private double _yOffset = 0;
    private string _gameId;
    private List<Challenge> _challenges;
    private string _inviteCode;
    public BingoBricka(string gameId)
    {
        InitializeComponent();
        _gameId = gameId;
        _challenges = new();
        _inviteCode = string.Empty;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        BingoGame cachedGame = null;

        // 🧠 1. Läs från cache
        var cachedJson = Preferences.Get($"cachedGame_{_gameId}", null);
        if (!string.IsNullOrEmpty(cachedJson))
        {
            cachedGame = JsonSerializer.Deserialize<BingoGame>(cachedJson);
            if (cachedGame != null)
            {
                _inviteCode = cachedGame.InviteCode;
                _challenges = Converters.ConvertBingoCardsToChallenges(cachedGame.Cards);
                InviteCodeLabel.Text = _inviteCode;
                PopulateBingoGrid(_challenges);
            }
        }

        try
        {
            // 🔄 2. Hämta färsk data i bakgrunden
            var latestGame = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            if (latestGame != null)
            {
                var shouldRefresh = cachedGame == null ||
                                    cachedGame.Cards?.Count != latestGame.Cards?.Count ||
                                    !cachedGame.Cards.Select(c => c.Title).SequenceEqual(latestGame.Cards.Select(c => c.Title));

                if (shouldRefresh)
                {
                    _challenges = Converters.ConvertBingoCardsToChallenges(latestGame.Cards);
                    PopulateBingoGrid(_challenges);
                }

                _inviteCode = latestGame.InviteCode;
                InviteCodeLabel.Text = _inviteCode;

                // 💾 Uppdatera cache
                Preferences.Set($"cachedGame_{_gameId}", JsonSerializer.Serialize(latestGame));
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
                await Application.Current.MainPage.DisplayAlert("Fel", "Inga utmaningar att visa.", "OK");
                return;
            }

            BingoGrid.Children.Clear();
            BingoGrid.RowDefinitions.Clear();
            BingoGrid.ColumnDefinitions.Clear();

            int totalItems = challenges.Count;
            int gridSize = (int)Math.Ceiling(Math.Sqrt(totalItems));

            for (int i = 0; i < gridSize; i++)
            {
                BingoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
                BingoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            for (int i = 0; i < totalItems; i++)
            {
                var challenge = challenges[i];
                int row = i / gridSize;
                int col = i % gridSize;

                var tileGrid = new Grid
                {
                    BackgroundColor = Colors.Purple,
                    Padding = 4,
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
                    HorizontalTextAlignment = TextAlignment.Center
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

                    var userId = App.CurrentUserProfile?.UserId;
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

                // Tap för hela rutan
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

                BingoGrid.Add(tileGrid, col, row);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel vid generering av bingobricka: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Fel", "Kunde inte bygga bingobrickan.", "OK");
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
        if (text.Length > 30)
            return 8; // Väldigt lång text
        else if (text.Length > 20)
            return 10; // Medellång text
        else
            return 12; // Kort text
    }
    private async void OnShowLeaderboardClicked(object sender, EventArgs e)
    {
        // Navigera till LeaderboardPage och skicka med _gameId
        await Navigation.PushAsync(new BingoMaui.Leaderboard(_gameId));
    }
    private async void OnToggleCommentsClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new CommentModal(_gameId));
    }
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            // Starta skalningen
            _startScale = BingoGrid.Scale;
            BingoGrid.AnchorX = 0;
            BingoGrid.AnchorY = 0;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // Räkna ut ny skalning
            double currentScale = Math.Max(1, _startScale * e.Scale);
            BingoGrid.Scale = currentScale;

            // Håll offset inom området
            var deltaX = (_xOffset - e.ScaleOrigin.X) * (currentScale - 1) * BingoGrid.Width;
            var deltaY = (_yOffset - e.ScaleOrigin.Y) * (currentScale - 1) * BingoGrid.Height;

            BingoGrid.TranslationX = Math.Min(0, Math.Max(deltaX, -BingoGrid.Width * (currentScale - 1)));
            BingoGrid.TranslationY = Math.Min(0, Math.Max(deltaY, -BingoGrid.Height * (currentScale - 1)));
        }
        else if (e.Status == GestureStatus.Completed)
        {
            // Spara offset och skalning
            _xOffset = BingoGrid.TranslationX;
            _yOffset = BingoGrid.TranslationY;
            _currentScale = BingoGrid.Scale;
        }
    }
    private async Task ShowAllCompletedPlayers(List<CompletedInfo> completedList)
    {
        string playerNames = string.Join("\n", completedList.Select(c =>
            $"{c.Nickname} ({c.UserColor})"));

        await Application.Current.MainPage.DisplayAlert("Spelare som klarat denna:", playerNames, "OK");
    }

}
