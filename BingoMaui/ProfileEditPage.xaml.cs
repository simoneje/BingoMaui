using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using BingoMaui.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui;
using BingoMaui.Services.Backend;

namespace BingoMaui
{
    public partial class ProfileEditPage : ContentPage
    {
        private UserProfile _profile;

        public ProfileEditPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            string userId = await BackendServices.GetUserIdAsync();
            _profile = await BackendServices.MiscService.GetUserProfileFromApiAsync();

            if (_profile != null)
            {
                if (!string.IsNullOrEmpty(_profile.ProfileImageUrl))
                    ProfileImage.Source = ImageSource.FromUri(new Uri(_profile.ProfileImageUrl));

                BioEditor.Text = _profile.Bio;
                AgeEntry.Text = _profile.Age?.ToString();
                GoalEntry.Text = _profile.Goal;
                InterestsEntry.Text = _profile.Interests;
                GenderEntry.Text = _profile.Gender;

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

            var success = await BackendServices.MiscService.UpdateUserProfileAsync(_profile);
            if (success)
                await DisplayAlert("Sparat", "Din profil har sparats!", "OK");
            else
                await DisplayAlert("Fel", "Kunde inte spara profilen.", "OK");
        }

        private async void OnChangeProfilePictureClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Välj en profilbild",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    var imageUrl = await BackendServices.MiscService.UploadProfileImageAsync(stream, result.FileName);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        _profile.ProfileImageUrl = imageUrl;
                        ProfileImage.Source = ImageSource.FromUri(new Uri(imageUrl));

                        await BackendServices.MiscService.UpdateUserProfileAsync(_profile);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Fel", $"Kunde inte ladda upp bilden: {ex.Message}", "OK");
            }
        }

        private List<BadgeModel> GetBadgesFromIds(List<string> achievementIds)
        {
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