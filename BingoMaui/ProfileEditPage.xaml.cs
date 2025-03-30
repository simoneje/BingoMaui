using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using BingoMaui.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui;

namespace BingoMaui
{
    public partial class ProfileEditPage : ContentPage
    {
        private readonly FirestoreService _firestoreService = new();
        private UserProfile _profile;

        public ProfileEditPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            ProfileImage.Source = "dotnet_bot.png";
            var userId = Preferences.Get("UserId", string.Empty);
            _profile = await _firestoreService.GetUserProfileAsync(userId);

            if (_profile != null)
            {
                if (!string.IsNullOrEmpty(_profile.ProfileImageUrl))
                    ProfileImage.Source = ImageSource.FromUri(new Uri(_profile.ProfileImageUrl));

                BioEditor.Text = _profile.Bio;
                AgeEntry.Text = _profile.Age?.ToString();
                GoalEntry.Text = _profile.Goal;
                InterestsEntry.Text = _profile.Interests;
                GenderEntry.Text = _profile.Gender;

                // Bind achievements (optional placeholder)
                BadgesCollection.ItemsSource = GetBadgesFromIds(_profile.Achievements);
            }
        }

        private async void OnSaveProfileClicked(object sender, EventArgs e)
        {
            _profile.Bio = BioEditor.Text;
            _profile.Goal = GoalEntry.Text;
            _profile.Interests = InterestsEntry.Text;
            _profile.Gender = GenderEntry.Text;
            

            if (int.TryParse(AgeEntry.Text, out int age))
                _profile.Age = age;

            await _firestoreService.UpdateUserProfileAsync(_profile);
            await DisplayAlert("Sparat", "Din profil har sparats!", "OK");
        }

        private async void OnChangeProfilePictureClicked(object sender, EventArgs e)
        {
            try
            {
                await DisplayAlert("Finns ej", "Inte implementerat än", "OK");
                //var result = await FilePicker.PickAsync(new PickOptions
                //{
                //    PickerTitle = "Välj en profilbild",
                //    FileTypes = FilePickerFileType.Images
                //});

                //if (result != null)
                //{
                //    var stream = await result.OpenReadAsync();
                //    var downloadUrl = await _firestoreService.UploadProfileImageAsync(_profile.UserId, stream);
                //    _profile.ProfileImageUrl = downloadUrl;
                //    ProfileImage.Source = ImageSource.FromUri(new Uri(downloadUrl));
                //}
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fel vid uppladdning av profilbild: {ex.Message}");
                await DisplayAlert("Fel", "Kunde inte ladda upp profilbild.", "OK");
            }
        }

        private List<BadgeModel> GetBadgesFromIds(List<string> achievementIds)
        {
            // Placeholder: Replace with real logic
            var allBadges = new List<BadgeModel>
            {
                new BadgeModel { Id = "firstWin", Name = "Första vinsten", Icon = "firstwin.png" },
                new BadgeModel { Id = "completed10Challenges", Name = "10 klara!", Icon = "10done.png" }
            };

            return allBadges.FindAll(b => achievementIds.Contains(b.Id));
        }
    }

    public class BadgeModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; } // name of image in Resources/Images
    }
}