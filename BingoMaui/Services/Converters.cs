using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingoMaui.Services
{
    internal class Converters
    {
        public static BingoCard ConvertDictionaryToBingoCard(Dictionary<string, object> dict)
        {
            return new BingoCard
            {
                CardId = dict.ContainsKey("ChallengeId") ? dict["ChallengeId"].ToString() : Guid.NewGuid().ToString(),
                Title = dict["Title"]?.ToString(),
                Description = dict["Description"]?.ToString(),
                Category = dict["Category"]?.ToString(),
                CompletedBy = new List<CompletedInfo>() // tom från användarskapade
            };
        }

        public static BingoCard ConvertChallengeToBingoCard(Challenge challenge)
        {
            return new BingoCard
            {
                CardId = challenge.ChallengeId,
                Title = challenge.Title,
                Description = challenge.Description,
                Category = challenge.Category,
                CompletedBy = challenge.CompletedBy ?? new()
            };
        }
        public static List<Challenge> ConvertBingoCardsToChallenges(List<BingoCard> bingoCards)
        {
            return bingoCards.Select(card => new Challenge
            {
                ChallengeId = card.CardId,
                Title = card.Title,
                Description = card.Description,
                Category = card.Category,
                CompletedBy = card.CompletedBy
            }).ToList();
        }
    }

}
