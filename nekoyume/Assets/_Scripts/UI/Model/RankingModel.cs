using System.Collections.Generic;
using JetBrains.Annotations;

namespace Nekoyume.UI.Model
{
#region Legacy
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
        public long Cp;
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
#endregion Legacy

#region Mimir
    public class GameObjectInfo
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
    }

    public class AvatarInfo
    {
        public int? ArmorId { get; set; }
        public int? PortraitId { get; set; }
        public GameObjectInfo Object { get; set; }
    }

    public class UserDocument
    {
        public int Cp { get; set; }
        public string Id { get; set; }
    }

    public class AdventureCpRankingItem
    {
        public AvatarInfo Avatar { get; set; }
        public long Cp { get; set; }
    }

    public class AdventureCpRankingData
    {
        public List<AdventureCpRankingItem> Items { get; set; }
    }

    public class AdventureCpRankingResponse
    {
        public AdventureCpRankingData AdventureCpRanking { get; set; }
    }

    public class MyAdventureCpRankingData
    {
        public int? Rank { get; set; }
        [CanBeNull]
        public UserDocument UserDocument { get; set; }
    }

    public class MyAdventureCpRankingResponse
    {
        public MyAdventureCpRankingData MyAdventureCpRanking { get; set; }
    }
    public class StageRankingItem
    {
        public int LastStageClearedId { get; set; }
        [CanBeNull]
        public AvatarInfo Avatar { get; set; }
    }

    public class WorldInformationRankingData
    {
        public List<StageRankingItem> Items { get; set; }
    }

    public class WorldInformationRankingResponse
    {
        public WorldInformationRankingData WorldInformationRanking { get; set; }
    }

    public class MyWorldInformationRankingData
    {
        public int? Rank { get; set; }
        [CanBeNull]
        public MyWorldUserDocument UserDocument { get; set; }
    }

    public class MyWorldUserDocument
    {
        public string Id { get; set; }
        public int LastStageClearedId { get; set; }
    }

    public class MyWorldInformationRankingResponse
    {
        public MyWorldInformationRankingData MyWorldInformationRanking { get; set; }
    }

#endregion Mimir
}
