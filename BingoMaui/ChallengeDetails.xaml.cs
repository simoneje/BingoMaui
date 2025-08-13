using BingoMaui.Services;
using BingoMaui;
using BingoMaui.Services.Backend;

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
            _currentUserId = await BackendServices.GetUserIdAsync(); // 🔁 Använd SecureStorage-hjälpare om du har den
            await LoadCompletedPlayers();
        }
        private async Task LoadCompletedPlayers()
        {
            try
            {
                var playerList = new List<DisplayPlayer>();

                if (_challenge.CompletedBy != null)
                {
                    var game = await BackendServices.GameService.GetGameByIdAsync(_gameId);

                    foreach (var entry in _challenge.CompletedBy)
                    {
                        if (game.PlayerInfo.TryGetValue(entry.PlayerId, out var stats))
                        {
                            playerList.Add(new DisplayPlayer
                            {
                                Nickname = stats.Nickname,
                                Color = stats.Color,
                                PlayerId = entry.PlayerId
                            });
                        }
                    }

                    CompletedPlayersList.ItemsSource = playerList;
                }
                else
                {
                    CompletedPlayersList.ItemsSource = null;
                }
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
            if (_challenge.CompletedBy != null && _challenge.CompletedBy.Any(c => c.PlayerId == _currentUserId))
            {
                await DisplayAlert("Info", "Du har redan klarat denna utmaning!", "OK");
                return;
            }

            if (string.IsNullOrEmpty(_gameId) || string.IsNullOrEmpty(_challenge.Title))
            {
                await DisplayAlert("Fel", "Kunde inte identifiera spelet eller utmaningen.", "OK");
                return;
            }

            var success = await BackendServices.ChallengeService.MarkChallengeAsCompletedAsync(_gameId, _challenge.Title);
            if (!success)
            {
                await DisplayAlert("Fel", "Det gick inte att markera utmaningen som klarad.", "OK");
                return;
            }

            var updatedGame = await BackendServices.GameService.GetGameByIdAsync(_gameId);
            if (updatedGame == null)
            {
                await DisplayAlert("Fel", "Kunde inte hämta spelet efter uppdatering.", "OK");
                return;
            }

            var updatedChallenges = Converters.ConvertBingoCardsToChallenges(updatedGame.Cards);
            var updatedCard = updatedChallenges.FirstOrDefault(c => c.Title == _challenge.Title);

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

            var success = await BackendServices.ChallengeService.UnmarkChallengeAsCompletedAsync(_gameId, _challenge.Title);

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
            var updatedCard = updatedChallenges.FirstOrDefault(c => c.Title == _challenge.Title);

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
