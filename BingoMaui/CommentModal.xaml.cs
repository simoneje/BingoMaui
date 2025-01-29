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
        // Ladda kommentarer n�r modalsidan �ppnas
        LoadComments();
    }
    private async Task RefreshCommentsAsync()
    {
        try
        {
            // Uppdatera kommentarerna fr�n Firebase
            await LoadComments();

            // Signalera att uppdateringen �r klar
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
            // H�mta kommentarerna fr�n Firestore
            var comments = await _firestoreService.GetCommentsAsync(_gameId);

            // Sortera kommentarerna (�ldsta f�rst)
            comments.Sort((c1, c2) => c2.Timestamp.CompareTo(c1.Timestamp));

            // Formatiera tiden p� varje kommentar
            foreach (var comment in comments)
            {
                comment.FormattedTime = _firestoreService.FormatTimeAgo(comment.Timestamp);
            }

            CommentsListView.ItemsSource = null; // T�m f�rst
            CommentsListView.ItemsSource = comments; // S�tt den nya sorterade listan
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading comments: {ex.Message}");
        }
        
        
    }

    private async void OnPostCommentClicked(object sender, EventArgs e)
    {
        // Kontrollera att inmatningen inte �r tom
        if (string.IsNullOrWhiteSpace(CommentEntry.Text))
        {
            await DisplayAlert("Fel", "Kommentaren kan inte vara tom!", "OK");
            return;
        }

        await _firestoreService.PostCommentAsync(_gameId, Preferences.Get("UserId", string.Empty), CommentEntry.Text);

        // Rensa textf�ltet och uppdatera listan med kommentarer
        CommentEntry.Text = string.Empty;
        LoadComments();
    }
    private async void OnCloseModalClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync(); // St�nger modal-sidan
    }
}
