using System;
using Microsoft.Maui.Controls;
using BingoMaui.Services;
using System.Threading.Tasks;
using BingoMaui.Services.Backend;

namespace BingoMaui
{
    public partial class ProfilePublicPage : ContentPage
    {
        private readonly FirestoreService _firestoreService = new();
        private string _userId;

        public ProfilePublicPage(string userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            ProfileImage.Source = "dotnet_bot.png";
            var profile = await BackendServices.MiscService.GetUserProfileFromApiAsync();
            if (profile != null)
            {
                NicknameLabel.Text = profile.Nickname;
                BioLabel.Text = profile.Bio;
                AgeLabel.Text = profile.Age?.ToString() ?? "-";
                GoalLabel.Text = profile.Goal;
                InterestsLabel.Text = profile.Interests;
                GenderLabel.Text = profile.Gender;

                if (!string.IsNullOrWhiteSpace(profile.ProfileImageUrl))
                {
                    ProfileImage.Source = ImageSource.FromUri(new Uri(profile.ProfileImageUrl));
                }

                // TODO: Ladda badges och visa
            }
        }
    }
}