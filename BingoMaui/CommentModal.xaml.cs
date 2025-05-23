using BingoMaui;
using BingoMaui.Components;
using BingoMaui.Services;
using CommunityToolkit.Maui.Views;
using System.Windows.Input;
namespace BingoMaui;
public partial class CommentModal : ContentPage
{
    private readonly string _gameId;
    private readonly FirestoreService _firestoreService;
    public ICommand NavigateToPublicProfileCommand { get; }
    public Command RefreshCommand { get; }

    public CommentModal(string gameId)
    {
        InitializeComponent();
        _gameId = gameId;
        _firestoreService = new FirestoreService();
        // Definiera RefreshCommand
        RefreshCommand = new Command(async () => await RefreshCommentsAsync());
        CommentsRefreshView.Command = RefreshCommand;
        // Ladda kommentarer när modalsidan öppnas
        NavigateToPublicProfileCommand = new Command<string>(NavigateToPublicProfile);

        BindingContext = this; // Viktigt! Annars hittar XAML inte kommandot
        LoadComments();
    }
    private async Task RefreshCommentsAsync()
    {
        try
        {
            // Uppdatera kommentarerna från Firebase
            await LoadComments();

            // Signalera att uppdateringen är klar
            CommentsRefreshView.IsRefreshing = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing comments: {ex.Message}");
            CommentsRefreshView.IsRefreshing = false;
        }
    }
    private async Task LoadComments()
    {
        try
        {
            // Hämta kommentarerna från Firestore
            var comments = await _firestoreService.GetCommentsAsync(_gameId);

            // Hämta spelet för att komma åt PlayerInfo
            var game = await _firestoreService.GetGameByIdAsync(_gameId);

            foreach (var comment in comments)
            {
                // Formatiera tiden på varje kommentar
                comment.FormattedTime = _firestoreService.FormatTimeAgo(comment.Timestamp);

                // Försök hämta spelarens färg från spelets PlayerInfo
                if (game.PlayerInfo.TryGetValue(comment.UserId, out var stats))
                {
                    comment.PlayerColor = stats.Color;
                }
            }

            // Sortera kommentarerna (äldsta först)
            comments.Sort((c1, c2) => c2.Timestamp.CompareTo(c1.Timestamp));

            // Uppdatera UI
            CommentsListView.ItemsSource = null;
            CommentsListView.ItemsSource = comments;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading comments: {ex.Message}");
        }
    }

    private async void OnPostCommentClicked(object sender, EventArgs e)
    {
        // Kontrollera att inmatningen inte är tom
        if (string.IsNullOrWhiteSpace(CommentEntry.Text))
        {
            await DisplayAlert("Fel", "Kommentaren kan inte vara tom!", "OK");
            return;
        }

        await _firestoreService.PostCommentAsync(_gameId, Preferences.Get("UserId", string.Empty), CommentEntry.Text);

        // Rensa textfältet och uppdatera listan med kommentarer
        CommentEntry.Text = string.Empty;
        LoadComments();
    }
    private async void OnCloseModalClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(); // Stänger modal-sidan
    }
    private async void NavigateToPublicProfile(string userId)
    {
        try
        {

        
        Console.WriteLine($"Navigerar till profil för användare: {userId}");
        if (string.IsNullOrEmpty(userId))
            return;
        // Stäng denna modal först
        await Navigation.PopModalAsync();
        var profilePage = new ProfilePublicPage(userId);
        Application.Current.MainPage.Navigation.PushAsync(profilePage);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    private async void OnMoreOptionsClicked(object sender, EventArgs e)
    {
        var popup = new MoreOptionsPopup(_gameId);
        await this.ShowPopupAsync(popup);
    }
}
