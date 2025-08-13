using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics.Platform;
using BingoMaui.Services;

namespace BingoMaui
{
    public partial class ImageCropPage : ContentPage
    {
        private readonly Stream _imageStream;


        public ImageCropPage(Stream imageStream)
        {
            InitializeComponent();
            _imageStream = imageStream;
            LoadImage();
        }

        private void LoadImage()
        {
            ImageToCrop.Source = ImageSource.FromStream(() => _imageStream);
        }

        //private async void OnConfirmCropClicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var croppedStream = await CropImageFromViewAsync();

        //        var downloadUrl = await _firestoreService.UploadProfileImageAsync(croppedStream, App.CurrentUserProfile.UserId);

        //        App.CurrentUserProfile.ProfileImageUrl = downloadUrl;

        //        await DisplayAlert("Lyckades", "Profilbilden är uppdaterad!", "OK");

        //        await Navigation.PopAsync(); // tillbaks till profilinställningar
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Fel", $"Kunde inte beskära och ladda upp bilden: {ex.Message}", "OK");
        //    }
        //}

        //private async Task<Stream> CropImageFromViewAsync()
        //{
        //    var handler = ImageToCrop.Handler;
        //    var platformView = handler?.PlatformView as Microsoft.Maui.Platform.PlatformView;

        //    if (platformView == null)
        //        throw new Exception("Kunde inte hämta plattformsvyn för beskärning.");

        //    var screenshot = await platformView.CaptureAsync();
        //    var croppedBitmap = screenshot?.Crop(new Rect(100, 100, 300, 300)); // justera efter din crop-ruta

        //    if (croppedBitmap == null)
        //        throw new Exception("Kunde inte beskära bilden.");

        //    var ms = new MemoryStream();
        //    croppedBitmap.Save(ms, ImageFormat.Png);
        //    ms.Position = 0;
        //    return ms;
        //}
    }
}