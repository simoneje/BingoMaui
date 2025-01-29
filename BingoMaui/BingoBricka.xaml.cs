using Firebase;
using BingoMaui.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
namespace BingoMaui;

public partial class BingoBricka : ContentPage
{

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
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PopulateBingoGrid(_challenges);
        //await LoadComments();
    }
    //private async Task LoadBingoCardAsync()
    //{
    //    // 1. H�mta spelet fr�n Firestore
    //    var game = await _firestoreService.GetGameByIdAsync(_gameId);
    //    if (game == null)
    //    {
    //        Console.WriteLine($"Game with ID {_gameId} not found.");
    //        await DisplayAlert("Error", "Spelet kunde inte laddas.", "OK");
    //        return;
    //    }

    //    // 2. Kontrollera om spelet har n�gra "Cards"
    //    if (game.Cards == null || game.Cards.Count == 0)
    //    {
    //        Console.WriteLine("No cards found for the game.");
    //        await DisplayAlert("Error", "Inga bingobrickor hittades f�r spelet.", "OK");
    //        return;
    //    }

    //    // 3. H�mta detaljerna f�r utmaningarna baserat p� Cards
    //    var challenges = await _firestoreService.GetChallengesForGameAsync(_gameId);

    //    // 4. Om inga utmaningar hittas, visa ett fel
    //    if (challenges == null || challenges.Count == 0)
    //    {
    //        Console.WriteLine("No challenges found for the game.");
    //        await DisplayAlert("Error", "Inga utmaningar hittades f�r spelet.", "OK");
    //        return;
    //    }

    //    // 5. Fyll bingobrickan med detaljerade utmaningar
    //    PopulateBingoGrid(challenges);

    //    Console.WriteLine($"Loaded game: {game.GameName}, Challenges: {challenges.Count}");
    //}
    private async void PopulateBingoGrid(List<Challenge> challenges)
    {
        try
        {
            // Kontrollera att BingoGrid inte �r null
            if (BingoGrid == null)
            {
                Console.WriteLine("Error: BingoGrid �r null.");
                return;
            }

            // Kontrollera att challenges-listan inte �r null
            if (challenges == null || challenges.Count == 0)
            {
                Console.WriteLine("Error: Challenge-listan �r null eller tom.");
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

                    // Kontrollera att challenge inte �r null
                    if (challenge == null)
                    {
                        Console.WriteLine($"Error: Challenge vid index {index} �r null.");
                        continue;
                    }

                    // Skapa knappen
                    var button = new Button
                    {
                        Text = challenge.Title ?? "Ok�nd utmaning",
                        FontSize = CalculateFontSize(challenge.Title),
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        VerticalOptions = LayoutOptions.FillAndExpand,
                        BackgroundColor = Colors.Purple,
                        TextColor = Colors.White,
                        Padding = new Thickness(5),
                        TextTransform = TextTransform.None,
                        LineBreakMode = LineBreakMode.WordWrap
                    };

                    // Klick-h�ndelse: Navigera till ChallengeDetails
                    button.Clicked += async (sender, args) =>
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(_gameId))
                            {
                                Console.WriteLine("Error: _gameId �r null eller tom.");
                                return;
                            }

                            var challengeDetailsPage = new ChallengeDetails(_gameId, challenge);
                            await Navigation.PushAsync(challengeDetailsPage);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error navigating to ChallengeDetails: {ex.Message}");
                            await Application.Current.MainPage.DisplayAlert("Fel", "Ett fel intr�ffade vid navigering till utmaningsdetaljer.", "OK");
                        }
                    };

                    // L�gg till knappen i grid
                    BingoGrid.Add(button, col, row);
                    index++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error populating BingoGrid: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Fel", "Ett fel intr�ffade n�r bingobrickan skulle fyllas.", "OK");
        }
    }

    //private async Task LoadBingoGridAsync(string gameId)
    //{
    //    // H�mta utmaningarna fr�n Firebase
    //    var challenges = await _firestoreService.GetChallengesForGameAsync(gameId);

    //    // Fyll bingobrickan med utmaningarna
    //    PopulateBingoGrid(challenges);
    //}
    private double CalculateFontSize(string text)
    {
        if (text.Length > 30)
            return 8; // V�ldigt l�ng text
        else if (text.Length > 20)
            return 10; // Medell�ng text
        else
            return 12; // Kort text
    }

    //private async Task LoadComments()
    //{
    //    try
    //    {
    //        _comments = await _firestoreService.GetCommentsAsync(_gameId);
    //        CommentsListView.ItemsSource = _comments;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error loading comments: {ex.Message}");
    //    }
    //}
    //private async void OnPostCommentClicked(object sender, EventArgs e)
    //{
    //    string message = CommentEntry.Text;
    //    if (string.IsNullOrWhiteSpace(message))
    //    {
    //        await DisplayAlert("Fel", "Du kan inte posta en tom kommentar!", "OK");
    //        return;
    //    }

    //    await _firestoreService.PostCommentAsync(_gameId, App.LoggedInNickname, message);

    //    CommentEntry.Text = ""; // Rensa f�ltet efter postning
    //    await LoadComments(); // Ladda om kommentarer
    //}
    private async void OnToggleCommentsClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new CommentModal(_gameId));
    }
}
