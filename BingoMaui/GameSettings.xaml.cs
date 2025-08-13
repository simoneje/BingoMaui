using BingoMaui.Services;

namespace BingoMaui;

public partial class GameSettings : ContentPage
{
    private string _gameId;
    private string _playerId;
    private Dictionary<string, PlayerStats> _playerInfo;
    private string _selectedColor;

    public GameSettings(string gameId, Dictionary<string, PlayerStats> playerInfo)
    {
        InitializeComponent();
        _gameId = gameId;
        _playerInfo = playerInfo;

        GameNameLabel.Text = $"Spel-ID: {gameId}";
        ColorPicker.ItemsSource = new List<string>
        {
            "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF"
        };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _playerId = await BackendServices.GetUserIdAsync(); // 🟢 Använder SecureStorage
        if (_playerInfo.TryGetValue(_playerId, out var stats))
        {
            _selectedColor = stats.Color;
            ColorPicker.SelectedItem = _selectedColor; // Förifyll färgen
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedColor))
        {
            await DisplayAlert("Fel", "Du måste välja en färg.", "OK");
            return;
        }

        var success = await BackendServices.GameService.UpdatePlayerColorInGameAsync(_gameId, _selectedColor);

        if (success)
        {
            await DisplayAlert("Klart!", "Din färg har uppdaterats för detta spel.", "OK");
            await Navigation.PopAsync(); // Gå tillbaka
        }
        else
        {
            await DisplayAlert("Fel", "Det gick inte att uppdatera färgen.", "OK");
        }
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
