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
        _playerId = Preferences.Get("UserId", string.Empty);
        _playerInfo = playerInfo;

        GameNameLabel.Text = $"Spel-ID: {gameId}";
        var AvailableColors = new List<string> { "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" };

        if (_playerInfo.TryGetValue(_playerId, out var stats))
            _selectedColor = stats.Color;

        ColorPicker.ItemsSource = AvailableColors;
        ColorPicker.SelectedItem = _selectedColor; // F�rifylld
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedColor))
        {
            await DisplayAlert("Fel", "Du m�ste v�lja en f�rg.", "OK");
            return;
        }

        var success = await BackendServices.GameService.UpdatePlayerColorInGameAsync(_gameId, _selectedColor);

        if (success)
        {
            await DisplayAlert("Klart!", "Din f�rg har uppdaterats f�r detta spel.", "OK");
            await Navigation.PopAsync(); // G� tillbaka
        }
        else
        {
            await DisplayAlert("Fel", "Det gick inte att uppdatera f�rgen.", "OK");
        }
    }

    private void OnColorSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is string selected)
        {
            _selectedColor = selected;
            Console.WriteLine($"Vald f�rg: {_selectedColor}");
        }
    }

    private void OnColorTapped(object sender, EventArgs e)
    {
        if (sender is Frame frame && frame.BackgroundColor is Color color)
        {
            _selectedColor = color.ToHex();
            Console.WriteLine($"F�rg tappad: {_selectedColor}");
        }
    }
}
