using Microsoft.Maui.Controls;

namespace BingoMaui.Components
{
    public partial class MoreOptionsButton : ContentView
    {
        public event EventHandler Clicked;

        public MoreOptionsButton()
        {
            InitializeComponent();
        }

        private void OnClicked(object sender, EventArgs e)
        {
            Clicked?.Invoke(this, e);
        }
    }
}