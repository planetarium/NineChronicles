using Nekoyume.Model.State;

namespace Nekoyume.UI.Model
{
    public class RankingModel
    {
        public int Rank;
        public AvatarState AvatarState;
    }

    public class AbilityRankingModel : RankingModel
    {
        public int Cp;
    }

    public class StageRankingModel : RankingModel
    {
        public int Stage;
    }
}
