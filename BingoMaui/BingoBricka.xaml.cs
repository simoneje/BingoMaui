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
    private readonly FirestoreService _firestoreService;
    private string _gameId;
    private List<Challenge> _challenges;
    private List<Comment> _comments = new List<Comment>();
    public BingoBricka(string gameId, List<Challenge> challenges)
    {
        InitializeComponent();
        _firestoreService = new FirestoreService(); // Skapa en instans av tjänstklassen
        _gameId = gameId; // ID för specifika spelet som visas
        _challenges = challenges;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var game = await _firestoreService.GetGameByIdAsync(_gameId);
        if (game != null)
        {
            InviteCodeLabel.Text = game.InviteCode; // Uppdatera InviteCode på UI
        }
        PopulateBingoGrid(_challenges);
        if (!BingoGrid.GestureRecognizers.OfType<PinchGestureRecognizer>().Any())
        {
            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += OnPinchUpdated;

            BingoGrid.GestureRecognizers.Add(pinchGesture); // Lägg till gesten på bingobrickan
        }

        
        //await LoadComments();
    }
    private async void PopulateBingoGrid(List<Challenge> challenges)
    {
        try
        {
            // Kontrollera att BingoGrid inte är null
            if (BingoGrid == null)
            {
                Console.WriteLine("Error: BingoGrid är null.");
                return;
            }

            // Kontrollera att challenges-listan inte är null
            if (challenges == null || challenges.Count == 0)
            {
                Console.WriteLine("Error: Challenge-listan är null eller tom.");
                await Application.Current.MainPage.DisplayAlert("Fel", "Inga utmaningar att visa.", "OK");
                return;
            }

            BingoGrid.Children.Clear();
            int index = 0;

            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    if (index >= challenges.Count)
                        break;

                    var challenge = challenges[index];

                    // Skapa en Grid (eller Frame) som ersätter Button
                    var tileGrid = new Grid
                    {
                        BackgroundColor = Colors.Purple,
                        Padding = 5,
                        RowDefinitions =
                    {
                        new RowDefinition { Height = GridLength.Star }, // Titel
                        new RowDefinition { Height = GridLength.Auto }, // Cirklar
                    }
                    };

                    // --- Rad 1: Titel ---
                    var titleLabel = new Label
                    {
                        Text = challenge.Title ?? "Okänd utmaning",
                        FontSize = CalculateFontSize(challenge.Title),
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        VerticalOptions = LayoutOptions.CenterAndExpand,
                        TextColor = Colors.White,
                        LineBreakMode = LineBreakMode.WordWrap
                    };
                    tileGrid.Add(titleLabel, 0, 0);

                    // --- Rad 2: Färgprickar (StackLayout) ---
                    var dotsLayout = new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        Spacing = 2
                    };

                    // Om du vill begränsa antalet cirklar, kan du göra:
                    // int maxDots = 5; 
                    // var completedCount = challenge.CompletedBy?.Count ?? 0;
                    // int displayedDots = Math.Min(completedCount, maxDots);

                    if (challenge.CompletedBy != null)
                    {
                        // Exempel: visa alla CompletedBy som cirklar
                        foreach (var completedInfo in challenge.CompletedBy)
                        {
                            // Om du har UserColor som hex-sträng, använd den här
                            var colorHex = string.IsNullOrWhiteSpace(completedInfo.UserColor)
                                           ? "#FFFFFF" // fallback-färg
                                           : completedInfo.UserColor;

                            var dot = new BoxView
                            {
                                WidthRequest = 10,
                                HeightRequest = 10,
                                CornerRadius = 5, // hälften av bredd/höjd => cirkel
                                BackgroundColor = Color.FromArgb(colorHex),
                                Margin = new Thickness(2)
                            };
                            dotsLayout.Children.Add(dot);
                        }

                        // Om du vill visa "..." när fler än x spelare klarat utmaningen:
                        // if (completedCount > maxDots)
                        // {
                        //     dotsLayout.Children.Add(new Label { Text = "...", TextColor = Colors.White });
                        // }
                    }

                    tileGrid.Add(dotsLayout, 0, 1);

                    // --- Lägg till en TapGestureRecognizer för klick ---
                    var tapGesture = new TapGestureRecognizer();
                    tapGesture.Tapped += async (sender, args) =>
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(_gameId))
                            {
                                Console.WriteLine("Error: _gameId är null eller tom.");
                                return;
                            }

                            var challengeDetailsPage = new ChallengeDetails(_gameId, challenge);
                            await Navigation.PushAsync(challengeDetailsPage);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error navigating to ChallengeDetails: {ex.Message}");
                            await Application.Current.MainPage.DisplayAlert("Fel", "Ett fel inträffade vid navigering till utmaningsdetaljer.", "OK");
                        }
                    };
                    tileGrid.GestureRecognizers.Add(tapGesture);

                    // Lägg till "cellen" i BingoGrid
                    BingoGrid.Add(tileGrid, col, row);
                    index++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error populating BingoGrid: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Fel", "Ett fel inträffade när bingobrickan skulle fyllas.", "OK");
        }
    }
    private async void PPopulateBingoGrid(List<Challenge> challenges)
    {
        try
        {
            // Kontrollera att BingoGrid inte är null
            if (BingoGrid == null)
            {
                Console.WriteLine("Error: BingoGrid är null.");
                return;
            }

            // Kontrollera att challenges-listan inte är null
            if (challenges == null || challenges.Count == 0)
            {
                Console.WriteLine("Error: Challenge-listan är null eller tom.");
                await Application.Current.MainPage.DisplayAlert("Fel", "Inga utmaningar att visa.", "OK");
                return;
            }

            BingoGrid.Children.Clear();
            int index = 0;

            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    if (index >= challenges.Count)
                        break;

                    var challenge = challenges[index];

                    // Kontrollera att challenge inte är null
                    if (challenge == null)
                    {
                        Console.WriteLine($"Error: Challenge vid index {index} är null.");
                        continue;
                    }

                    // Skapa knappen
                    var button = new Button
                    {
                        Text = challenge.Title ?? "Okänd utmaning",
                        FontSize = CalculateFontSize(challenge.Title),
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        BackgroundColor = Colors.Purple,
                        TextColor = Colors.White,
                        Padding = new Thickness(5),
                        TextTransform = TextTransform.None,
                        LineBreakMode = LineBreakMode.WordWrap
                    };

                    // Klick-händelse: Navigera till ChallengeDetails
                    button.Clicked += async (sender, args) =>
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(_gameId))
                            {
                                Console.WriteLine("Error: _gameId är null eller tom.");
                                return;
                            }

                            var challengeDetailsPage = new ChallengeDetails(_gameId, challenge);
                            await Navigation.PushAsync(challengeDetailsPage);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error navigating to ChallengeDetails: {ex.Message}");
                            await Application.Current.MainPage.DisplayAlert("Fel", "Ett fel inträffade vid navigering till utmaningsdetaljer.", "OK");
                        }
                    };

                    // Lägg till knappen i grid
                    BingoGrid.Add(button, col, row);
                    index++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error populating BingoGrid: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Fel", "Ett fel inträffade när bingobrickan skulle fyllas.", "OK");
        }
    }

    //private async Task LoadBingoGridAsync(string gameId)
    //{
    //    // Hämta utmaningarna från Firebase
    //    var challenges = await _firestoreService.GetChallengesForGameAsync(gameId);

    //    // Fyll bingobrickan med utmaningarna
    //    PopulateBingoGrid(challenges);
    //}
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
}
