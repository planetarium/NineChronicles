using Nekoyume.Model.State;
using System.Collections.Generic;

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
        public int ClearedStageId;
    }

    public class EquipmentRankingModel : RankingModel
    {
        public int Level;
        public int Cp;
    }

    public class StageRankingResponse
    {
        public List<StageRankingRecord> StageRanking;
    }

    public class RankingRecord
    {
        public string AvatarAddress;
    }

    public class StageRankingRecord : RankingRecord
    {
        public int ClearedStageId;
    }
}
