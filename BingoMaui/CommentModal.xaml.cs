using BingoMaui;
using BingoMaui.Services;
namespace BingoMaui;
public partial class CommentModal : ContentPage
{
    private readonly string _gameId;
    private readonly FirestoreService _firestoreService;
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

            // Sortera kommentarerna (äldsta först)
            comments.Sort((c1, c2) => c2.Timestamp.CompareTo(c1.Timestamp));

            // Formatiera tiden på varje kommentar
            foreach (var comment in comments)
            {
                comment.FormattedTime = _firestoreService.FormatTimeAgo(comment.Timestamp);
            }

            CommentsListView.ItemsSource = null; // Töm först
            CommentsListView.ItemsSource = comments; // Sätt den nya sorterade listan
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
}
