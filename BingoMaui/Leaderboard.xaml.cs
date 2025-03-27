using Firebase;
using BingoMaui;
using Google.Cloud.Firestore;
using BingoMaui.Services;
using System.Collections.ObjectModel;
namespace BingoMaui;

public partial class Leaderboard : ContentPage
{
    private readonly FirestoreService _firestoreService;
    private string _gameId;
    private ObservableCollection<LeaderboardEntry> _leaderboardEntries = new ObservableCollection<LeaderboardEntry>();
    public Leaderboard(string gameId)
    {
        InitializeComponent();
        _gameId = gameId;
        _firestoreService = new FirestoreService();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLeaderboardAsync();
    }
    

    private async Task LoadLeaderboardAsync()
    {
        var leaderboard = await _firestoreService.GetLeaderboardAsync(_gameId);
        var sortedEntries = leaderboard
            .OrderByDescending(kvp => kvp.Value)
            .Select(async kvp =>
            {
                var nickname = await _firestoreService.GetUserNicknameAsync(kvp.Key);
                return new LeaderboardEntry { Nickname = nickname, Points = kvp.Value };
            });

        var entries = await Task.WhenAll(sortedEntries);

        // Rensa den observerbara samlingen och lägg in nya värden
        _leaderboardEntries.Clear();
        foreach (var entry in entries)
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
