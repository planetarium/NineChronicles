using System.Collections.Generic;

namespace Nekoyume.UI.Model
{
    public class WorldBossRankingRecord
    {
        public int Ranking;
        public int Level;
        public int Cp;
        public int IconId;
        public string AvatarName;
        public string Address;
        public int HighScore;
        public int TotalScore;
    }

    public class worldBossRanking
    {
        public long BlockIndex;
        public List<WorldBossRankingRecord> RankingInfo;
    }

    public class WorldBossRankingResponse
    {
        public int WorldBossTotalUsers;
        public worldBossRanking WorldBossRanking;
    }

    // public class WorldBossRankingResponse
    // {
    //     public List<WorldBossRankingRecord> WorldBossRanking;
    //     public int WorldBossTotalUsers;
    // }
}
