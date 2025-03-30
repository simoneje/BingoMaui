using BingoMaui.Services;

namespace BingoMaui;

public partial class GameSettings : ContentPage
{
    private string _gameId;
    private string _playerId;
    private Dictionary<string, PlayerStats> _playerInfo;
    private string _selectedColor;
    private readonly FirestoreService _firestoreService;

    public GameSettings(string gameId, Dictionary<string, PlayerStats> playerInfo)
    {
        InitializeComponent();
        _gameId = gameId;
        _playerId = Preferences.Get("UserId", string.Empty);
        _playerInfo = playerInfo;
        _firestoreService = new FirestoreService();

        GameNameLabel.Text = $"Spel-ID: {gameId}";
        var AvailableColors = new List<string> { "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" };

        if (_playerInfo.TryGetValue(_playerId, out var stats))
            _selectedColor = stats.Color;
        ColorPicker.ItemsSource = AvailableColors;
    }
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedColor))
        {
            await DisplayAlert("Fel", "Du måste välja en färg.", "OK");
            return;
        }

        // Uppdatera i Firestore
        await _firestoreService.UpdatePlayerColorInGameAsync(_gameId, _playerId, _selectedColor);

        await DisplayAlert("Klart!", "Din färg har uppdaterats för detta spel.", "OK");
        await Navigation.PopAsync(); // Gå tillbaka till brickan
    }
    private void OnColorSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selected)
        {
            _selectedColor = selected;
            Console.WriteLine($"Vald färg: {_selectedColor}");
        }
    }
    private void OnColorTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BackgroundColor is Color color)
        {
            _selectedColor = color.ToHex();
            Console.WriteLine($"Färg tappad: {_selectedColor}");
        }
    }
}