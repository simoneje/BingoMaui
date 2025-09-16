using BingoMaui.Services;
using BingoMaui.Services.Backend;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BingoMaui
{
    public partial class ChallengeDetails : ContentPage
    {
        private readonly Challenge _challenge;
        private string _currentUserId;
        private readonly string _gameId;

        public ChallengeDetails(string gameId, Challenge challenge)
        {
            InitializeComponent();
            _challenge = challenge;
            _gameId = gameId;

            ChallengeTitleLabel.Text = challenge.Title;
            ChallengeDescriptionLabel.Text = challenge.Description;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 0) Hämta aktuell användare
            _currentUserId = await BackendServices.GetUserIdAsync();


            // 1) Uppdatera challenge.CompletedBy från senaste kända state
            await RefreshChallengeFromLatestGame();

            // 2) Ladda spelare som klarat
            await LoadCompletedPlayers();
        }

        private async Task RefreshChallengeFromLatestGame()
        {
            // Försök först cache (snabbt), annars backend
            var game = AccountServices.LoadGameFromCache(_gameId)
                       ?? await BackendServices.GameService.GetGameByIdAsync(_gameId);

            if (game == null || game.Cards == null) return;

            var all = Converters.ConvertBingoCardsToChallenges(game.Cards);

            // Matcha i första hand på CardId (ChallengeId)
            var latest = all.FirstOrDefault(c => c.ChallengeId == _challenge.ChallengeId)
                        // fallback för äldre data om CardId saknades
                        ?? all.FirstOrDefault(c => string.Equals(c.Title, _challenge.Title, StringComparison.OrdinalIgnoreCase));

            if (latest != null)
                _challenge.CompletedBy = latest.CompletedBy ?? new List<CompletedInfo>();
        }

        private async Task LoadCompletedPlayers()
        {
            try
            {
                var playerList = new List<DisplayPlayer>();

                if (_challenge.CompletedBy != null && _challenge.CompletedBy.Count > 0)
                {
                    var game = await BackendServices.GameService.GetGameByIdAsync(_gameId);
                    if (game?.PlayerInfo != null)
                    {
                        foreach (var entry in _challenge.CompletedBy)
                        {
                            if (game.PlayerInfo.TryGetValue(entry.PlayerId, out var stats) && stats != null)
                            {
                                playerList.Add(new DisplayPlayer
                                {
                                    Nickname = stats.Nickname,
                                    Color = stats.Color,
                                    PlayerId = entry.PlayerId
                                });
                            }
                        }
                    }
                }

                CompletedPlayersList.ItemsSource = playerList.Count > 0 ? playerList : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading players: {ex.Message}");
                await DisplayAlert("Fel", "Kunde inte ladda spelarinformation.", "OK");
            }
        }

        private async void OnPlayerSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is DisplayPlayer selected)
            {
                await Navigation.PushAsync(new ProfilePublicPage(selected.PlayerId));
            }
            ((CollectionView)sender).SelectedItem = null;
        }

        private async void OnMarkAsCompletedClicked(object sender, EventArgs e)
        {
            // Säkerställ färskt state så vi inte dubbelmarkerar i onödan
            await RefreshChallengeFromLatestGame();

            if (_challenge.CompletedBy != null && _challenge.CompletedBy.Any(c => c.PlayerId == _currentUserId))
            {
                await DisplayAlert("Info", "Du har redan klarat denna utmaning!", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_gameId) || string.IsNullOrEmpty(_challenge.ChallengeId))
            {
                await DisplayAlert("Fel", "Kunde inte identifiera spelet eller kortets CardId.", "OK");
                return;
            }

            var success = await BackendServices.ChallengeService
                .MarkChallengeAsCompletedAsync(_gameId, _challenge.ChallengeId);

            if (!success)
            {
                await DisplayAlert("Fel", "Det gick inte att markera utmaningen som klarad.", "OK");
                return;
            }

            // Hämta uppdaterat spel och uppdatera just denna ruta (CardId-first)
            var updatedGame = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            if (updatedGame == null)
            {
                await DisplayAlert("Fel", "Kunde inte hämta spelet efter uppdatering.", "OK");
                return;
            }

            var updatedChallenges = Converters.ConvertBingoCardsToChallenges(updatedGame.Cards);
            var updatedCard = updatedChallenges.FirstOrDefault(c => c.ChallengeId == _challenge.ChallengeId)
                              ?? updatedChallenges.FirstOrDefault(c => string.Equals(c.Title, _challenge.Title, StringComparison.OrdinalIgnoreCase));

            if (updatedCard == null)
            {
                await DisplayAlert("Fel", "Kunde inte hitta uppdaterad utmaning i spelet.", "OK");
                return;
            }

            _challenge.CompletedBy = updatedCard.CompletedBy;
            await LoadCompletedPlayers();
            await DisplayAlert("Framgång!", "Utmaningen är markerad som klarad!", "OK");
        }

        private async void OnUnmarkCompletedClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Bekräfta", "Vill du verkligen ta bort din klarmarkering?", "Ja", "Avbryt");
            if (!confirm) return;

            if (string.IsNullOrEmpty(_gameId) || string.IsNullOrEmpty(_challenge.ChallengeId))
            {
                await DisplayAlert("Fel", "Kunde inte identifiera spelet eller kortets CardId.", "OK");
                return;
            }

            var success = await BackendServices.ChallengeService
                .UnmarkChallengeAsCompletedAsync(_gameId, _challenge.ChallengeId);

            if (!success)
            {
                await DisplayAlert("Fel", "Det gick inte att avmarkera utmaningen.", "OK");
                return;
            }

            var updatedGame = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            if (updatedGame == null)
            {
                await DisplayAlert("Fel", "Kunde inte hämta spelet efter uppdatering.", "OK");
                return;
            }

            var updatedChallenges = Converters.ConvertBingoCardsToChallenges(updatedGame.Cards);
            var updatedCard = updatedChallenges.FirstOrDefault(c => c.ChallengeId == _challenge.ChallengeId)
                              ?? updatedChallenges.FirstOrDefault(c => string.Equals(c.Title, _challenge.Title, StringComparison.OrdinalIgnoreCase));

            if (updatedCard == null)
            {
                await DisplayAlert("Fel", "Kunde inte hitta uppdaterad utmaning i spelet.", "OK");
                return;
            }

            _challenge.CompletedBy = updatedCard.CompletedBy;
            await LoadCompletedPlayers();
            await DisplayAlert("Uppdaterat", "Din klarmarkering har tagits bort.", "OK");
        }
    }
}
