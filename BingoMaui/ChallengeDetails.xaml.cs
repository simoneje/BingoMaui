using BingoMaui.Services;
using BingoMaui;
using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace BingoMaui
{
    public partial class ChallengeDetails : ContentPage
    {
        private readonly Challenge _challenge; // Utmaningen vi visar
        private readonly FirestoreService _firestoreService;
        private readonly string _currentUserId;
        private readonly string _gameId; // Spelets ID

        public ChallengeDetails(string gameId, Challenge challenge)
        {
            InitializeComponent();
            _firestoreService = new FirestoreService();
            _challenge = challenge;
            _currentUserId = Preferences.Get("UserId", string.Empty); // Hämta inloggad användares ID
            _gameId = gameId;
            
            // Ladda spelare som klarat utmaningen
            LoadCompletedPlayers();
            // Visa data
            ChallengeTitleLabel.Text = challenge.Title;
            ChallengeDescriptionLabel.Text = challenge.Description;

            
        }
        private async void LoadCompletedPlayers()
        {
            try
            {
                var playerList = new List<DisplayPlayer>();

                if (_challenge.CompletedBy != null)
                {
                    var game = await _firestoreService.GetGameByIdAsync(_gameId);

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

            // Kontrollera att nödvändiga parametrar finns
            if (string.IsNullOrEmpty(_gameId) || string.IsNullOrEmpty(_challenge.Title))
            {
                await DisplayAlert("Fel", "Kunde inte identifiera spelet eller utmaningen.", "OK");
                return;
            }

            // Markera utmaningen som klarad
            await _firestoreService.MarkChallengeAsCompletedAsync(_gameId, _challenge.Title, _currentUserId);

            // Uppdatera UI
            if (_challenge.CompletedBy == null)
            {
                _challenge.CompletedBy = new List<CompletedInfo>();
            }

            if (!_challenge.CompletedBy.Any(c => c.PlayerId == _currentUserId))
            {
                var game = await _firestoreService.GetGameByIdAsync(_gameId);
                var currentUserColor = game.PlayerInfo[_currentUserId].Color;
                // Här sätter du den aktuella användarens färg – ersätt med din egen logik för att hämta en anpassad färg.

                _challenge.CompletedBy.Add(new CompletedInfo
                {
                    PlayerId = _currentUserId,
                    UserColor = currentUserColor
                });
            }

            // Uppdatera App.CompletedChallengesCache
            if (!App.CompletedChallengesCache.ContainsKey(_gameId))
            {
                App.CompletedChallengesCache[_gameId] = new Dictionary<string, List<string>>();
            }
            if (!App.CompletedChallengesCache[_gameId].ContainsKey(_challenge.Title))
            {
                App.CompletedChallengesCache[_gameId][_challenge.Title] = new List<string>();
            }

            // Hämta användarens nickname (du kan ha en egen logik för detta)
            var nickname = await _firestoreService.GetUserNicknameAsync(_currentUserId);
            if (!App.CompletedChallengesCache[_gameId][_challenge.Title].Contains(nickname))
            {
                App.CompletedChallengesCache[_gameId][_challenge.Title].Add(nickname);
            }
            LoadCompletedPlayers();

            await DisplayAlert("Framgång!", "Utmaningen är markerad som klarad!", "OK");
        }
        private async void OnUnmarkCompletedClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Bekräfta", "Vill du verkligen ta bort din klarmarkering?", "Ja", "Avbryt");
            if (!confirm) return;

            await _firestoreService.UnmarkChallengeAsCompletedAsync(_gameId, _challenge.Title, _currentUserId);

            // Uppdatera lokal modell
            _challenge.CompletedBy?.RemoveAll(c => c.PlayerId == _currentUserId);

            // Rensa från cache
            if (App.CompletedChallengesCache.ContainsKey(_gameId) &&
                App.CompletedChallengesCache[_gameId].ContainsKey(_challenge.Title))
            {
                var nickname = await _firestoreService.GetUserNicknameAsync(_currentUserId);
                App.CompletedChallengesCache[_gameId][_challenge.Title].Remove(nickname);
            }

            LoadCompletedPlayers();
            await DisplayAlert("Uppdaterat", "Din klarmarkering har tagits bort.", "OK");
        }
    }
}
