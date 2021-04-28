using Libplanet;
using Nekoyume.Model.State;

namespace Nekoyume.UI.Model
{
    public class RankingModel
    {
        public int Rank;
        public string Name;
        public Address AvatarAddress;
        public AvatarState AvatarState;
    }

    public class AbilityRankingModel : RankingModel
    {
        public int Cp;
        public int Level;
    }

    public class StageRankingModel : RankingModel
    {
        public int Stage;
    }
}
