using Firebase;
using BingoMaui;
using Google.Cloud.Firestore;
using BingoMaui.Services;
using System.Collections.ObjectModel;
namespace BingoMaui;

public partial class Leaderboard : ContentPage
{
    private string _gameId;
    private List<LeaderboardEntry> _entries = new();
    public Leaderboard(string gameId)
    {
        InitializeComponent();
        _gameId = gameId;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLeaderboardAsync();
    }
    private async Task LoadLeaderboardAsync()
    {
        var game = await BackendServices.GameService.GetGameByIdAsync(_gameId);
        if (game == null || game.PlayerInfo == null)
        {
            await DisplayAlert("Fel", "Kunde inte ladda spelet.", "OK");
            return;
        }

        var sorted = game.PlayerInfo
            .OrderByDescending(p => p.Value.Points)
            .Select((kvp, index) => new LeaderboardEntry
            {
                Nickname = kvp.Value.Nickname,
                Points = kvp.Value.Points,
                PlayerColor = Color.FromArgb(kvp.Value.Color ?? "#FFFFFF"),
                RankEmoji = index switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => $"{index + 1}."
                },
                PointsText = $"Poäng: {kvp.Value.Points}",
                PlayerId = kvp.Key
            })
            .ToList();

        _entries = sorted;
        LeaderboardCollectionView.ItemsSource = _entries;
    }
    private async void OnPlayerSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is LeaderboardEntry selected)
        {
            await Navigation.PushAsync(new ProfilePublicPage(selected.PlayerId));
        }

        ((CollectionView)sender).SelectedItem = null;
    }

    public class LeaderboardEntry
    {
        public string Nickname { get; set; }
        public int Points { get; set; }
        public string RankEmoji { get; set; }
        public string PointsText { get; set; }
        public Color PlayerColor { get; set; }
        public string PlayerId { get; set; }
    }
}

