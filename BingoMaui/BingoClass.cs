using Google.Cloud.Firestore;
using Plugin.CloudFirestore.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BingoMaui
{
    public class BingoGame
    {
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; }

        [JsonPropertyName("gameId")]
        public string GameId { get; set; }

        [JsonPropertyName("gameName")]
        public string GameName { get; set; }

        [JsonPropertyName("hostId")]
        public string HostId { get; set; }

        [JsonPropertyName("playerIds")]
        public List<string> PlayerIds { get; set; } = new();

        [JsonPropertyName("playerInfo")]
        public Dictionary<string, PlayerStats> PlayerInfo { get; set; } = new();

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("cards")]
        public List<BingoCard> Cards { get; set; }

        [JsonPropertyName("inviteCode")]
        public string InviteCode { get; set; }
    }
    public class BingoCard
    {
        [JsonPropertyName("cardId")]
        public string CardId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("completedBy")]
        public List<CompletedInfo> CompletedBy { get; set; } = new();
    }
    public class Challenge
    {
        [JsonPropertyName("challengeId")]
        public string ChallengeId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("completedBy")]
        public List<CompletedInfo> CompletedBy { get; set; } = new();
    }
    public class Comment
    {
        [JsonPropertyName("commentId")]
        public string CommentId { get; set; }

        [JsonPropertyName("gameId")]
        public string GameId { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("playerColor")]
        public string PlayerColor { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("reactions")]
        public Dictionary<string, List<string>> Reactions { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        // Dessa två används för visning i klienten, inte för API
        [JsonIgnore]
        public string FormattedTime { get; set; }

        [JsonIgnore]
        public string ReactionsDisplay => Reactions != null
            ? string.Join("  ", Reactions.Select(r => $"{r.Key} {r.Value.Count}"))
            : "";
    }
    public class CompletedInfo
    {
        [JsonPropertyName("playerId")]
        public string PlayerId { get; set; }

        [JsonPropertyName("userColor")]
        public string UserColor { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("proofImageUrl")]
        public string? ProofImageUrl { get; set; }

        [JsonPropertyName("witnessName")]
        public string? WitnessName { get; set; }
    }

    // Klass för PlayerInfo Dict
    public class PlayerStats
    {
        [JsonPropertyName("points")]
        public int Points { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
    }


    public class UserProfile
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("playerColor")]
        public string PlayerColor { get; set; }

        [JsonPropertyName("profileImageUrl")]
        public string ProfileImageUrl { get; set; }

        [JsonPropertyName("bio")]
        public string Bio { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("interests")]
        public string Interests { get; set; }

        [JsonPropertyName("goal")]
        public string Goal { get; set; }

        [JsonPropertyName("achievements")]
        public List<string> Achievements { get; set; } = new();
        public UserProfile() { }

    }
    public class DisplayPlayer
    {
        public string Nickname { get; set; }
        public string Color { get; set; }
        public string PlayerId { get; set; }
    }
}