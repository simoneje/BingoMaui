using Firebase;
using BingoMaui;
using Google.Cloud.Firestore;
using BingoMaui.Services;
using System.Collections.ObjectModel;
namespace BingoMaui;

public partial class Leaderboard : ContentPage
{
    private string _gameId;
    private ObservableCollection<LeaderboardEntry> _leaderboardEntries = new ObservableCollection<LeaderboardEntry>();
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

        var sortedEntries = game.PlayerInfo
            .OrderByDescending(p => p.Value.Points)
            .Select(p => new LeaderboardEntry
            {
                Nickname = p.Value.Nickname,
                Points = p.Value.Points
            })
            .ToList();

        _leaderboardEntries.Clear();
        foreach (var entry in sortedEntries)
        {
            _leaderboardEntries.Add(entry);
        }

        LeaderboardListView.ItemsSource = _leaderboardEntries;
    }


    public class LeaderboardEntry
    {
        public string Nickname { get; set; }
        public int Points { get; set; }
    }
}
