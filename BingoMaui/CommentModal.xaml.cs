using BingoMaui.Services;
using BingoMaui.Services.Backend;
using CommunityToolkit.Maui.Views;
using System.Windows.Input;
using BingoMaui.Components;

namespace BingoMaui;

public partial class CommentModal : ContentPage
{
    private readonly string _gameId;
    public ICommand NavigateToPublicProfileCommand { get; }
    public Command RefreshCommand { get; }

    public CommentModal(string gameId)
    {
        InitializeComponent();
        _gameId = gameId;

        RefreshCommand = new Command(async () => await RefreshCommentsAsync());
        CommentsRefreshView.Command = RefreshCommand;

        NavigateToPublicProfileCommand = new Command<string>(NavigateToPublicProfile);

        BindingContext = this;
        LoadComments();
    }

    private async Task RefreshCommentsAsync()
    {
        try
        {
            await LoadComments();
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
            var game = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            var comments = await BackendServices.CommentsService.GetCommentsAsync(_gameId);

            foreach (var comment in comments)
            {
                comment.FormattedTime = FormatTimeAgo(comment.Timestamp);

                if (game.PlayerInfo.TryGetValue(comment.UserId, out var stats))
                    comment.PlayerColor = stats.Color;
            }

            comments.Sort((c1, c2) => c2.Timestamp.CompareTo(c1.Timestamp));
            CommentsListView.ItemsSource = comments;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading comments: {ex.Message}");
        }
    }

    private async void OnPostCommentClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CommentEntry.Text))
        {
            await DisplayAlert("Fel", "Kommentaren kan inte vara tom!", "OK");
            return;
        }

        await BackendServices.CommentsService.PostCommentAsync(_gameId, CommentEntry.Text);

        CommentEntry.Text = string.Empty;
        await LoadComments();
    }

    private async void NavigateToPublicProfile(string userId)
    {
        try
        {
            Console.WriteLine($"Navigerar till profil för användare: {userId}");
            if (string.IsNullOrEmpty(userId)) return;

            await Navigation.PopModalAsync();
            var profilePage = new ProfilePublicPage(userId);
            await Application.Current.MainPage.Navigation.PushAsync(profilePage);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void OnCloseModalClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnMoreOptionsClicked(object sender, EventArgs e)
    {
        var popup = new MoreOptionsPopup(_gameId);
        await this.ShowPopupAsync(popup);
    }

    private string FormatTimeAgo(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp;

        if (timeSpan.TotalSeconds < 60)
            return "Just nu";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} min sedan";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} h sedan";
        return timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}
