using Libplanet.Crypto;
using Nekoyume.Model.Arena;

namespace Nekoyume.UI.Model
{
    public class ArenaParticipantModel
    {
        public Address AvatarAddr;
        public int Rank;
        public int Score;
        public int WinScore;
        public int LoseScore;
        public int Level;
        public string NameWithHash;
        public int Cp;
        public int PortraitId;
        public string GuildName;
    }
}
