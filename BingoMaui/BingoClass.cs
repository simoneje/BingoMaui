using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingoMaui
{
    [FirestoreData]
    public class Player
    {
        [FirestoreProperty]
        public string PlayerId { get; set; }  // Spelarens unika ID
        [FirestoreProperty]
        public string Name { get; set; }  // Spelarens namn
        [FirestoreProperty]
        public string GameId { get; set; }  // Spelet som spelaren är med i
        [FirestoreProperty]
        public string BingoCardId { get; set; }  // Id för spelaren's bingokort
        [FirestoreProperty]
        public bool HasWon { get; set; }  // Om spelaren har vunnit
    }
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
        public string Status { get; set; } // Status, t.ex. "Active", "Finished"

        [FirestoreProperty]
        public DateTime StartDate { get; set; } // Startdatum för spelet

        [FirestoreProperty]
        public DateTime EndDate { get; set; } // Slutdatum för spelet

        [FirestoreProperty]
        public List<BingoCard> Cards { get; set; } // Alla bingokort i spelet

        [FirestoreProperty]
        public List<string> Players { get; set; } // Lista över spelare i spelet
        [FirestoreProperty]
        public string InviteCode { get; set; } // Invite code för spelet 
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
        public Dictionary<string, bool> PlayerProgress { get; set; } = new Dictionary<string, bool>();
        // Key: PlayerId, Value: Om spelaren har klarat rutan
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

        [FirestoreProperty]
        public List<string> CompletedBy { get; set; } // Lista med spelare som klarat utmaningen
    }
    public class PlayerModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public bool IsInvited { get; set; } // Om spelaren är inbjuden
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
    public class User
    {
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
