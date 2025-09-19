using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Devices; // DeviceDisplay
using Microsoft.Maui.Controls;
using BingoMaui.Services;

namespace BingoMaui
{
    public partial class BingoBricka : ContentPage
    {
        private string _gameId;
        private List<Challenge> _challenges = new();
        private string _inviteCode = string.Empty;

        // UserId (för pricklogik m.m.)
        private string _currentUserId;

        // ---- Layout/scroll parametrar ----
        private const int BaseVisibleCols = 5;    // så många kolumner får plats utan scroll (behåll looken)
        private const double TileSpacing = 5;     // matchar XAML Grid.Row/ColumnSpacing
        private const double GridSidePadding = 20; // BingoGrid.Padding left+right (10 + 10)

        private double _tileSize = 0;             // beräknas utifrån vybredd & BaseVisibleCols
        private bool _layoutReady = false;        // när vi har mått

        public BingoBricka(string gameId)
        {
            InitializeComponent();
            _gameId = gameId;

            // Reagera på storleksändring (rotation, fönsterändring)
            SizeChanged += OnPageSizeChanged;
        }

        // ====== Layout helpers ======
        private double CalcTileSizeFromWidth(double widthLogical)
        {
            var totalSpacing = TileSpacing * (BaseVisibleCols - 1);
            var usable = widthLogical - GridSidePadding - totalSpacing;
            var size = Math.Floor(usable / BaseVisibleCols);

            if (size < 60) size = 60;
            if (size > 220) size = 220;
            return size;
        }

        private void EnsureTileSizeEarly()
        {
            if (_layoutReady && _tileSize > 0) return;

            double w = BoardScroll?.Width ?? this.Width;

            if (w <= 0)
            {
                var di = DeviceDisplay.MainDisplayInfo;
                // px → dp (logiska punkter)
                w = di.Width / di.Density;
                if (w <= 0) w = 360; // sista nödfall
            }

            _tileSize = CalcTileSizeFromWidth(w);
            _layoutReady = true;
        }

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            if (BoardScroll?.Width > 0)
            {
                var size = CalcTileSizeFromWidth(BoardScroll.Width);
                if (Math.Abs(_tileSize - size) > 0.1)
                {
                    _tileSize = size;
                    _layoutReady = true;

                    if (_challenges.Count > 0)
                        PopulateBingoGrid(_challenges);
                }
            }
        }

        // ====== Livscykel ======
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 0) UserId (lokalt via din helper)
            _currentUserId = await BackendServices.GetUserIdAsync();

            // 1) Sätt tile size direkt (rendera utan att vänta på SizeChanged)
            EnsureTileSizeEarly();

            // 2) Rendera snabbt från cache
            var cachedGame = AccountServices.LoadGameFromCache(_gameId);
            if (cachedGame != null)
            {
                _inviteCode = cachedGame.InviteCode;
                _challenges = Converters.ConvertBingoCardsToChallenges(cachedGame.Cards);
                InviteCodeLabel.Text = _inviteCode;
                PopulateBingoGrid(_challenges);
            }

            // 3) Hämta färskt & uppdatera vid behov
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
                        PopulateBingoGrid(_challenges);
                    }

                    _inviteCode = latestGame.InviteCode;
                    InviteCodeLabel.Text = _inviteCode;

                    // 4) Uppdatera cache
                    AccountServices.SaveGameToCache(latestGame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔌 Fel vid hämtning från backend: {ex.Message}");
            }
        }

        // ====== Rendering ======
        private async void PopulateBingoGrid(List<Challenge> challenges)
        {
            try
            {
                if (BingoGrid == null || challenges == null || challenges.Count == 0)
                {
                    await DisplayAlert("Fel", "Inga utmaningar att visa.", "OK");
                    return;
                }

                if (_tileSize <= 0)
                    EnsureTileSizeEarly();

                // Batcha för mindre layout thrash
                BingoGrid.BatchBegin();

                BingoGrid.Children.Clear();
                BingoGrid.RowDefinitions.Clear();
                BingoGrid.ColumnDefinitions.Clear();

                int totalItems = challenges.Count;
                int gridSize = (int)Math.Ceiling(Math.Sqrt(totalItems)); // NxN

                for (int r = 0; r < gridSize; r++)
                    BingoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int c = 0; c < gridSize; c++)
                    BingoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                const int chunk = 50; // släpp igenom UI var 25:e ruta

                for (int i = 0; i < totalItems; i++)
                {
                    var challenge = challenges[i];
                    int row = i / gridSize;
                    int col = i % gridSize;

                    var tileContainer = new ContentView
                    {
                        WidthRequest = _tileSize,
                        HeightRequest = _tileSize,
                        Padding = 0
                    };

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

                    // Prickar / +X
                    if (challenge.CompletedBy != null && challenge.CompletedBy.Count > 0)
                    {
                        var completions = challenge.CompletedBy;
                        var dotsGrid = new Grid
                        {
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            Padding = 0
                        };

                        var myId = !string.IsNullOrEmpty(_currentUserId)
                            ? _currentUserId
                            : App.CurrentUserProfile?.UserId;

                        var myCompletion = completions.FirstOrDefault(c => c.PlayerId == myId);
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

                    // Tap på hela rutan → ChallengeDetails med CardId
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += async (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(_gameId))
                        {
                            var page = new ChallengeDetails(_gameId, challenge);
                            await Navigation.PushAsync(page);
                        }
                    };
                    tileGrid.GestureRecognizers.Add(tapGesture);

                    tileContainer.Content = tileGrid;
                    BingoGrid.Add(tileContainer, col, row);

                    if ((i + 1) % chunk == 0)
                    {
                        BingoGrid.BatchCommit();
                        await Task.Yield();      // släpp igenom UI rendering
                        BingoGrid.BatchBegin();
                    }
                }

                BingoGrid.BatchCommit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fel vid generering av bingobricka: {ex.Message}");
                await DisplayAlert("Fel", "Kunde inte bygga bingobrickan.", "OK");
            }
        }

        // ====== Övriga befintliga handlers ======
        private double CalculateFontSize(string text)
        {
            if (string.IsNullOrEmpty(text)) return 12;
            if (text.Length > 30) return 8;
            if (text.Length > 20) return 10;
            return 12;
        }

        private async Task ShowAllCompletedPlayers(List<CompletedInfo> completedList)
        {
            string playerNames = string.Join("\n", completedList.Select(c =>
                $"{c.Nickname} ({c.UserColor})"));

            await Application.Current.MainPage.DisplayAlert("Spelare som klarat denna:", playerNames, "OK");
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

        private async void OnShowLeaderboardClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BingoMaui.Leaderboard(_gameId));
        }

        private async void OnToggleCommentsClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new CommentModal(_gameId));
        }
    }
}
