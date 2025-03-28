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
                var cachedPlayers = new List<string>();

                // Kontrollera om vi har lokalt cachade spelare för denna challenge
                if (App.CompletedChallengesCache.ContainsKey(_gameId) && App.CompletedChallengesCache[_gameId].ContainsKey(_challenge.Title))
                {
                    cachedPlayers = App.CompletedChallengesCache[_gameId][_challenge.Title];

                    // Uppdatera UI direkt med den cachade datan
                    CompletedPlayersList.ItemsSource = cachedPlayers;
                    Console.WriteLine($"Loaded completed players from cache for challenge: {_challenge.Title}");
                    return; // Avsluta metoden här om vi har cached data
                }

                // Om inga spelare finns i cachen, hämta från Firestore
                var updatedChallenge = await _firestoreService.GetChallengeByTitleAsync(_gameId, _challenge.Title);

                if (updatedChallenge?.CompletedBy != null && updatedChallenge.CompletedBy.Count > 0)
                {
                    var completedPlayers = new List<string>();

                    foreach (var completed in updatedChallenge.CompletedBy)
                    {
                        // Anta att "completed" är av typen CompletedInfo och har en property PlayerId
                        var nickname = await _firestoreService.GetUserNicknameAsync(completed.PlayerId);
                        completedPlayers.Add(nickname);
                    }

                    // Uppdatera UI med nicknames
                    CompletedPlayersList.ItemsSource = completedPlayers;

                    // Uppdatera lokal cache med de hämtade spelarna
                    if (!App.CompletedChallengesCache.ContainsKey(_gameId))
                    {
                        App.CompletedChallengesCache[_gameId] = new Dictionary<string, List<string>>();
                    }

                    if (!App.CompletedChallengesCache[_gameId].ContainsKey(_challenge.Title))
                    {
                        App.CompletedChallengesCache[_gameId][_challenge.Title] = new List<string>();
                    }

                    App.CompletedChallengesCache[_gameId][_challenge.Title] = completedPlayers;

                    Console.WriteLine($"Fetched and cached completed players for challenge: {_challenge.Title}");
                }
                else
                {
                    CompletedPlayersList.ItemsSource = null; // Ingen spelare har klarat utmaningen
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading completed players: {ex.Message}");
                await DisplayAlert("Fel", "Kunde inte hämta spelarinformation.", "OK");
            }
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

    }
}
