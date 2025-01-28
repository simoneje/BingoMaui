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
            _currentUserId = Preferences.Get("UserId", string.Empty); // H�mta inloggad anv�ndares ID
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
                // H�mta det uppdaterade Challenge-objektet fr�n Firestore
                var updatedChallenge = await _firestoreService.GetChallengeByTitleAsync(_gameId, _challenge.Title);

                if (updatedChallenge?.CompletedBy != null && updatedChallenge.CompletedBy.Count > 0)
                {
                    var completedPlayers = new List<string>();

                    // H�mta nicknames f�r varje spelare som har klarat utmaningen
                    foreach (var userId in updatedChallenge.CompletedBy)
                    {
                        var nickname = await _firestoreService.GetUserNicknameAsync(userId);
                        completedPlayers.Add(nickname);
                    }

                    // Uppdatera UI med nicknames
                    _challenge.CompletedBy = updatedChallenge.CompletedBy; // Uppdatera lokalt objekt
                    CompletedPlayersList.ItemsSource = completedPlayers; // Visa nicknames i UI
                }
                else
                {
                    CompletedPlayersList.ItemsSource = null; // Ingen spelare har klarat utmaningen
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading completed players: {ex.Message}");
                await DisplayAlert("Fel", "Kunde inte h�mta spelarinformation.", "OK");
            }
        }
        private async void OnMarkAsCompletedClicked(object sender, EventArgs e)
        {
            if (_challenge.CompletedBy != null && _challenge.CompletedBy.Contains(_currentUserId))
            {
                await DisplayAlert("Info", "Du har redan klarat denna utmaning!", "OK");
                return;
            }

            // Kontrollera att n�dv�ndiga parametrar finns
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
                _challenge.CompletedBy = new List<string>();
            }
            _challenge.CompletedBy.Add(_currentUserId);
            LoadCompletedPlayers();

            await DisplayAlert("Framg�ng!", "Utmaningen �r markerad som klarad!", "OK");
        }

    }
}
