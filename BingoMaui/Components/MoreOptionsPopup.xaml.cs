using CommunityToolkit.Maui.Views;
using Google.Cloud.Firestore;
using BingoMaui.Services;
using System.ComponentModel.Design;

namespace BingoMaui.Components
{
    public partial class MoreOptionsPopup : Popup
    {
        private readonly string _gameId;
        private readonly string _challengeTitle;
        private string _commentId;
        private readonly FirestoreService _firestoreService;
        public MoreOptionsPopup(string gameId)
        {
            InitializeComponent();
            _firestoreService = new FirestoreService();
            _gameId = gameId;
            // Starta liten
            PopupContent.Scale = 0.5;
            PopupContent.Opacity = 0;
            this.Opened += OnPopupOpened;
            
        }

        private async void OnPopupOpened(object sender, EventArgs e)
        {

            try
            {

                await Task.Delay(50);
                // Kör animation: fade + scale
                await Task.WhenAll(
                    PopupContent.FadeTo(1, 150, Easing.CubicIn),
                    PopupContent.ScaleTo(1, 250, Easing.CubicOut)
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Animation error: {ex.Message}");
            }
        }
        private async void OnReactClicked(object sender, EventArgs e)
        {
            var emoji = await Application.Current.MainPage.DisplayActionSheet(
                "Välj en reaktion", "Avbryt", null, "👍", "❤️", "😂", "🔥", "🎉");

            if (emoji == null || emoji == "Avbryt") return;
            var game = await _firestoreService.GetGameByIdAsync(_gameId);
            var comments = await _firestoreService.GetCommentsAsync(_gameId);
            foreach (var comment in comments)
            {
                _commentId = comment.CommentId; // Du måste ha en sådan property
            }
            if (_commentId != null)
            {
                // FirestoreService måste ha referenser till gameId, challengeTitle osv – skicka in dem!
                await _firestoreService.ToggleReactionAsync(game.DocumentId, _commentId, App.CurrentUserProfile.UserId, emoji);
            }
            Close(); // Stäng popup
        }


        private void OnDeleteClicked(object sender, EventArgs e)
        {
            // Här kan du lägga in logik för att ta bort kommentaren
            Console.WriteLine("Ta bort kommentaren!");
            Close();
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            Close();
        }
    }
}