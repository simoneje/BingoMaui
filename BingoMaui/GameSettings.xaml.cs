using BingoMaui.Services;
using BingoMaui.Services.Backend;
namespace BingoMaui;

public partial class GameSettings : ContentPage
{
    private string _gameId;
    private string _playerId = "";
    private Dictionary<string, PlayerStats> _playerInfo;
    private string _selectedColor = "";
    private bool _isHost = false;

    public GameSettings(string gameId, Dictionary<string, PlayerStats> playerInfo)
    {
        InitializeComponent();
        _gameId = gameId;
        _playerInfo = playerInfo ?? new();

        GameNameLabel.Text = $"Spel-ID: {gameId}";

        // Du kan köra både Picker och klickbara rutor – välj vad du vill använda.
        ColorPicker.ItemsSource = new List<string>
        {
            "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF"
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _playerId = await BackendServices.GetUserIdAsync();

        // förifyll din färg som tidigare
        if (!string.IsNullOrWhiteSpace(_playerId) && _playerInfo != null && _playerInfo.TryGetValue(_playerId, out var stats))
        {
            _selectedColor = stats.Color;
            ColorPicker.SelectedItem = _selectedColor;
        }

        // hämta spelet -> visa DangerZone om Host + fyll spelare
        try
        {
            var game = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            if (game != null)
            {
                _isHost = !string.IsNullOrWhiteSpace(game.HostId) &&
                          string.Equals(game.HostId, _playerId, StringComparison.Ordinal);

                DangerZone.IsVisible = _isHost;

                if (_isHost && game.PlayerInfo != null)
                {
                    var players = game.PlayerInfo
                        .Where(kv => kv.Key != game.HostId) // dölj värden själv
                        .Select(kv => new { PlayerId = kv.Key, Nickname = kv.Value.Nickname })
                        .OrderBy(x => x.Nickname)
                        .ToList();

                    PlayersList.ItemsSource = players;
                }
                else
                {
                    PlayersList.ItemsSource = null;
                }
            }
            else
            {
                DangerZone.IsVisible = false;
                PlayersList.ItemsSource = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Kunde inte hämta spel i GameSettings: {ex.Message}");
            DangerZone.IsVisible = false;
            PlayersList.ItemsSource = null;
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
            // Uppdatera ev. cache så UI känns direkt
            var cached = AccountServices.LoadGameFromCache(_gameId);
            if (cached?.PlayerInfo != null && !string.IsNullOrWhiteSpace(_playerId))
            {
                if (cached.PlayerInfo.TryGetValue(_playerId, out var s))
                {
                    s.Color = _selectedColor;
                    cached.PlayerInfo[_playerId] = s;
                    AccountServices.SaveGameToCache(cached);
                }
            }

            await DisplayAlert("Klart!", "Din färg har uppdaterats för detta spel.", "OK");
            await Navigation.PopAsync();
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
            ColorPicker.SelectedItem = _selectedColor; // håll Picker i synk om du använder båda
            Console.WriteLine($"Färg tappad: {_selectedColor}");
        }
    }

    // === Danger zone (delete) ===

    private void OnDeleteConfirmTextChanged(object sender, TextChangedEventArgs e)
    {
        // enable knappen endast om texten är "delete" (ej case sensitive)
        var ok = string.Equals(e.NewTextValue?.Trim(), "delete", StringComparison.OrdinalIgnoreCase);
        DeleteGameButton.IsEnabled = _isHost && ok;
    }

    private async void OnDeleteGameClicked(object sender, EventArgs e)
    {
        if (!_isHost)
        {
            await DisplayAlert("Ej behörig", "Endast spelvärden kan ta bort spelet.", "OK");
            return;
        }

        // (Extra) sista bekräftelse
        var really = await DisplayAlert("Ta bort spel",
            "Är du säker? Detta går inte att ångra och spelet försvinner för alla.",
            "Ta bort", "Avbryt");

        if (!really) return;

        DeleteGameButton.IsEnabled = false;

        var ok = await BackendServices.GameService.DeleteGameAsync(_gameId);
        if (!ok)
        {
            DeleteGameButton.IsEnabled = true;
            return;
        }

        // Rensa cache och navigera bort
        AccountServices.RemoveGameFromCache(_gameId);
        await DisplayAlert("Borttaget", "Spelet har tagits bort.", "OK");
        await Navigation.PopToRootAsync();
    }
    private async void OnKickPlayerClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string targetUserId)
        {
            var confirm = await DisplayAlert("Kicka spelare",
                "Är du säker på att du vill kicka denna spelare?",
                "Kicka", "Avbryt");
            if (!confirm) return;

            var removeProgress = RemoveProgressCheck.IsChecked;
            var removeComments = RemoveCommentsCheck.IsChecked;

            var ok = await BackendServices.GameService.KickPlayerAsync(_gameId, targetUserId, removeProgress, removeComments);
            if (!ok) return;

            // uppdatera cache lokalt så UI känns direkt
            var cached = AccountServices.LoadGameFromCache(_gameId);
            if (cached != null)
            {
                BackendGameService.ApplyKickLocally(cached, targetUserId, removeProgress);
                AccountServices.SaveGameToCache(cached);
            }

            await DisplayAlert("Klart", "Spelaren har kickats.", "OK"); 

            // ladda om listan snabbt
            var game = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            if (_isHost && game?.PlayerInfo != null)
            {
                var players = game.PlayerInfo
                    .Where(kv => kv.Key != game.HostId)
                    .Select(kv => new { PlayerId = kv.Key, Nickname = kv.Value.Nickname })
                    .OrderBy(x => x.Nickname)
                    .ToList();

                PlayersList.ItemsSource = players;
            }
            else
            {
                PlayersList.ItemsSource = null;
            }
        }
    }

}
