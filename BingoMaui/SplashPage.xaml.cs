namespace BingoMaui
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await App.InitializeAsync();

            var isLoggedInRaw = await SecureStorage.GetAsync("IsLoggedIn");
            bool isLoggedIn = isLoggedInRaw == "true";


            if (isLoggedIn && App.CurrentUserProfile != null)
            {
                Application.Current.MainPage = new NavigationPage(new StartPage());
            }
            else
            {
                Application.Current.MainPage = new AppShell(); // Inloggningssida
            }
        }
    }
}
