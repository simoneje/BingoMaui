using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingoMaui
{

    [FirestoreData]
    public class BingoGame
    {
        [FirestoreProperty]
        public string DocumentId { get; set; } // Unikt ID för Firebase Documentet
        [FirestoreProperty]
        public string GameId { get; set; } // Unikt ID för spelet

        [FirestoreProperty]
        public string GameName { get; set; } // Namn på spelet

        [FirestoreProperty]
        public string HostId { get; set; } // Vem som skapade spelet

        [FirestoreProperty]
        public List<string> PlayerIds { get; set; } = new();

        [FirestoreProperty]
        public Dictionary<string, PlayerStats> PlayerInfo { get; set; } = new(); // Leaderboard

        [FirestoreProperty]
        public string Status { get; set; } // Status, t.ex. "Active", "Finished"

        [FirestoreProperty]
        public DateTime StartDate { get; set; } // Startdatum för spelet

        [FirestoreProperty]
        public DateTime EndDate { get; set; } // Slutdatum för spelet

        [FirestoreProperty]
        public List<BingoCard> Cards { get; set; } // Alla bingokort i spelet

        [FirestoreProperty]
        public string InviteCode { get; set; } // Invite code för spelet 

        public BingoGame() { }
    }

    [FirestoreData]
    public class BingoCard
    {
        [FirestoreProperty]
        public string CardId { get; set; } // Unikt ID för kortet

        [FirestoreProperty]
        public string Title { get; set; } // Titel för bingorutan

        [FirestoreProperty]
        public string Description { get; set; } // Beskrivning av utmaningen

        [FirestoreProperty]
        public string Category { get; set; } // Kategori för utmaningen

        [FirestoreProperty]
        public List<CompletedInfo> CompletedBy { get; set; } = new();

        public BingoCard() { }
    }
    [FirestoreData]
    public class Challenge
    {
        [FirestoreProperty]
        public string ChallengeId { get; set; } // Unik ID för utmaningen

        [FirestoreProperty]
        public string Title { get; set; }       // Titel på utmaningen

        [FirestoreProperty]
        public string Description { get; set; } // Beskrivning av utmaningen

        [FirestoreProperty]
        public string Category { get; set; }   // Kategori, tex "Träning", "Hälsa"

        [FirestoreProperty]
        public bool IsCompleted { get; set; }  // Om användaren har klarat den

        // Uppdaterad property med CompletedInfo istället för strängar
        [FirestoreProperty]
        public List<CompletedInfo> CompletedBy { get; set; } = new List<CompletedInfo>();
    }
    [FirestoreData]
    public class Comment
    {
        [FirestoreProperty]
        public string GameId { get; set; }

        [FirestoreProperty]
        public string Nickname { get; set; }

        [FirestoreProperty]
        public string Message { get; set; }

        [FirestoreProperty]
        public DateTime Timestamp { get; set; }

        [FirestoreProperty]
        public string FormattedTime { get; set; }

        public Comment() { } // Parameterlös konstruktor krävs för Firestore
    }
    [FirestoreData]
    public class CompletedInfo
    {
        [FirestoreProperty]
        public string PlayerId { get; set; }  // Spelarens unika ID

        [FirestoreProperty]
        public string UserColor { get; set; } // Exempelvis "#FF0000" för röd

        public CompletedInfo() { }
    }
    // Klass för PlayerInfo Dict
    [FirestoreData]
    public class PlayerStats
    {
        [FirestoreProperty]
        public int Points { get; set; }  // Spelarens unika ID

        [FirestoreProperty]
        public string Color { get; set; } // Exempelvis "#FF0000" för röd

        public PlayerStats() { }
    }
    [FirestoreData]
    public class UserProfile
    {
        [FirestoreProperty]
        public string UserId { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string Nickname { get; set; }

        [FirestoreProperty]
        public string PlayerColor { get; set; }  // t.ex. "#FF5733"

        public UserProfile() { }
    }
}