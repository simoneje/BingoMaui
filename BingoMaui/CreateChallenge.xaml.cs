using BingoMaui.Services;
namespace BingoMaui;

public partial class CreateChallenge : ContentPage
{
    private List<Dictionary<string, object>> userChallenges;
    private readonly FirestoreService _firestoreService;

    public CreateChallenge(List<Dictionary<string, object>> existingChallenges)
    {
        InitializeComponent();
        userChallenges = existingChallenges;
        ChallengesList.ItemsSource = userChallenges;
    }

    private void OnAddChallengeClicked(object sender, EventArgs e)
    {
        string newChallenge = ChallengeEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(newChallenge))
        {
            userChallenges.Add(new Dictionary<string, object> { { "Title", newChallenge } });
            ChallengesList.ItemsSource = null; // Uppdatera listan
            ChallengesList.ItemsSource = userChallenges;
            ChallengeEntry.Text = string.Empty; // Rensa inmatningsfältet
        }
        else
        {
            DisplayAlert("Fel", "Utmaningen kan inte vara tom.", "OK");
        }
    }

}
