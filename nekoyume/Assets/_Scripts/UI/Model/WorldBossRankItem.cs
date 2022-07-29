using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class WorldBossRankItem : IItemViewModel
    {
        public RectTransform View { get; set; }
        public int Ranking { get; }
        public string AvatarName { get; }
        public int HighScore { get; }
        public int TotalScore { get; }
        public int Cp { get; }
        public int Level { get; }
        public int Portrait { get; }

        public WorldBossRankItem(WorldBossRankingRecord record)
        {
            AvatarName = record.AvatarName;
            Ranking = record.Ranking;
            HighScore = record.HighScore;
            TotalScore = record.TotalScore;
            Cp = record.Cp;
            Level = record.Level;
            Portrait = record.IconId;
        }
    }
}
