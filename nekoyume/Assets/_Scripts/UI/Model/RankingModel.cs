using Nekoyume.Model.State;
using System.Collections.Generic;

namespace Nekoyume.UI.Model
{
    public class RankingModel
    {
        public int? Rank;
        public string AvatarAddress;
        public string Name;
        public int AvatarLevel;
        public int ArmorId;
        public int? TitleId;
    }

    public class AbilityRankingModel : RankingModel
    {
        public int Cp;
    }

    public class StageRankingModel : RankingModel
    {
        public int ClearedStageId;
    }

    public class CraftRankingModel : RankingModel
    {
        public int CraftCount;
    }

    public class EquipmentRankingModel : RankingModel
    {
        public int Level;
        public int Cp;
        public int EquipmentId;
    }

    public class AbilityRankingResponse
    {
        public List<AbilityRankingRecord> AbilityRanking;
    }

    public class StageRankingResponse
    {
        public List<StageRankingRecord> StageRanking;
    }

    public class CraftRankingResponse
    {
        public List<CraftRankingRecord> CraftRanking;
    }

    public class EquipmentRankingResponse
    {
        public List<EquipmentRankingRecord> EquipmentRanking;
    }

    public class RankingRecord
    {
        public int? Ranking;
        public string AvatarAddress;
        public string Name;
        public int AvatarLevel;
        public int ArmorId;
        public int? TitleId;
    }

    public class AbilityRankingRecord : RankingRecord
    {
        public int Cp;
    }

    public class StageRankingRecord : RankingRecord
    {
        public int ClearedStageId;
    }

    public class CraftRankingRecord : RankingRecord
    {
        public int CraftCount;
    }

    public class EquipmentRankingRecord : RankingRecord
    {
        public int Level;
        public int Cp;
        public int EquipmentId;
    }
}
