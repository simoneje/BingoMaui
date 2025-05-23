using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BingoMaui.Services.Backend.RequestModels
{
    public class CreateGameRequest
    {
        public string GameName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<BingoCard> Cards { get; set; }

        public string Nickname { get; set; }
        public string PlayerColor { get; set; }
    }
    public class JoinGameRequest
    {
        public string InviteCode { get; set; }
        public string PlayerColor { get; set; }
        public string Nickname { get; set; }
    }
    public class UpdateNicknameRequest
    {
        public string NewNickname { get; set; }
    }

    public class UpdateColorRequest
    {
        public string NewColor { get; set; }
    }
    public class ChallengeActionRequest
    {
        public string GameId { get; set; }
        public string ChallengeTitle { get; set; }
    }
    public class UpdateNicknameBatchRequest
    {
        public string NewNickname { get; set; }
    }

    public class SyncNicknameRequest
    {
        public string NewNickname { get; set; }
    }
}
