using Firebase;
using BingoMaui.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
namespace BingoMaui;

public partial class BingoBricka : ContentPage
{
    private double _currentScale = 1;
    private double _startScale = 1;
    private double _xOffset = 0;
    private double _yOffset = 0;
    private string _inviteCode;
    private readonly FirestoreService _firestoreService;
    private string _gameId;
    private List<Challenge> _challenges;
    private List<Comment> _comments = new List<Comment>();
    public BingoBricka(string gameId, List<Challenge> challenges)
    {
        InitializeComponent();
        _firestoreService = new FirestoreService(); // Skapa en instans av tj�nstklassen
        _gameId = gameId; // ID f�r specifika spelet som visas
        _challenges = challenges;
        _inviteCode = string.Empty;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (App.ShouldRefreshChallenges)
        {
            var game = await _firestoreService.GetGameByIdAsync(_gameId);
            if (game != null)
            {
                _inviteCode = game.InviteCode;
            }
            var updatedChallenges = _firestoreService.ConvertBingoCardsToChallenges(game.Cards);
            _challenges = updatedChallenges;
            App.ShouldRefreshChallenges = false;
        }
        InviteCodeLabel.Text = _inviteCode; // Uppdatera InviteCode p� UI
        PopulateBingoGrid(_challenges);
        if (!BingoGrid.GestureRecognizers.OfType<PinchGestureRecognizer>().Any())
        {
            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;

            BingoGrid.GestureRecognizers.Add(pinchGesture); // L�gg till gesten p� bingobrickan
        }

        
        //await LoadComments();
    }
    private async void PopulateBingoGrid(List<Challenge> challenges)
    {
        try
        {
            if (BingoGrid == null || challenges == null || challenges.Count == 0)
            {
                Console.WriteLine("BingoGrid �r null eller inga utmaningar.");
                await Application.Current.MainPage.DisplayAlert("Fel", "Inga utmaningar att visa.", "OK");
                return;
            }

            BingoGrid.Children.Clear();
            BingoGrid.RowDefinitions.Clear();
            BingoGrid.ColumnDefinitions.Clear();

            int totalItems = challenges.Count;
            int gridSize = (int)Math.Ceiling(Math.Sqrt(totalItems)); // t.ex. 10 f�r 100 rutor

            // Dynamiskt definiera rader & kolumner
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
                    new RowDefinition { Height = GridLength.Star },   // Titel
                    new RowDefinition { Height = GridLength.Auto },   // Prickar
                }
                };

                // Titel
                var titleLabel = new Label
                {
                    Text = challenge.Title ?? "Ok�nd utmaning",
                    FontSize = CalculateFontSize(challenge.Title),
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.WordWrap,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                tileGrid.Add(titleLabel, 0, 0);

                // Prickar
                var dotsLayout = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Spacing = 2
                };

                if (challenge.CompletedBy != null && challenge.CompletedBy.Count > 0)
                {
                    const int maxDots = 5;
                    int shownDots = Math.Min(maxDots, challenge.CompletedBy.Count);

                    for (int j = 0; j < shownDots; j++)
                    {
                        var completedInfo = challenge.CompletedBy[j];
                        var dotColor = string.IsNullOrWhiteSpace(completedInfo.UserColor) ? "#FFFFFF" : completedInfo.UserColor;

                        dotsLayout.Children.Add(new BoxView
                        {
                            WidthRequest = 10,
                            HeightRequest = 10,
                            CornerRadius = 5,
                            BackgroundColor = Color.FromArgb(dotColor),
                            Margin = new Thickness(1)
                        });
                    }

                    // L�gg till en "..."-indikator om fler �n maxDots
                    if (challenge.CompletedBy.Count > maxDots)
                    {
                        dotsLayout.Children.Add(new Label
                        {
                            Text = "...",
                            TextColor = Colors.White,
                            FontSize = 10,
                            VerticalOptions = LayoutOptions.Center,
                            Margin = new Thickness(2, 0)
                        });
                    }
                }

                tileGrid.Add(dotsLayout, 0, 1);

                // Tap Gesture
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
            var game = await _firestoreService.GetGameByIdAsync(_gameId);
            if (game == null)
            {
                await DisplayAlert("Fel", "Kunde inte h�mta spelet.", "OK");
                return;
            }

            // Navigera till GameSettingsPage med PlayerInfo
            await Navigation.PushAsync(new GameSettings(_gameId, game.PlayerInfo));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fel vid navigering till inst�llningar: {ex.Message}");
            await DisplayAlert("Fel", "Kunde inte �ppna inst�llningar f�r spelet.", "OK");
        }
    }

    private double CalculateFontSize(string text)
    {
        if (text.Length > 30)
            return 8; // V�ldigt l�ng text
        else if (text.Length > 20)
            return 10; // Medell�ng text
        else
            return 12; // Kort text
    }
    private async void OnShowLeaderboardClicked(object sender, EventArgs e)
    {
        // Navigera till LeaderboardPage och skicka med _gameId
        await Navigation.PushAsync(new Leaderboard(_gameId));
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
            // R�kna ut ny skalning
            double currentScale = Math.Max(1, _startScale * e.Scale);
            BingoGrid.Scale = currentScale;

            // H�ll offset inom omr�det
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
}
