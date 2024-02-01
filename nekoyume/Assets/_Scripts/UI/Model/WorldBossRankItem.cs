using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class WorldBossRankItem
    {
        public WorldBossCharacterSheet.Row BossRow;
        public RectTransform View { get; set; }
        public int Ranking { get; }
        public string AvatarName { get; }
        public string Address { get; }
        public long HighScore { get; }
        public long TotalScore { get; }
        public int Cp { get; }
        public int Level { get; }
        public int Portrait { get; }

        public WorldBossRankItem(WorldBossCharacterSheet.Row row, WorldBossRankingRecord record)
        {
            BossRow = row;
            AvatarName = record.AvatarName;
            Address = record.Address;
            Ranking = record.Ranking;
            HighScore = record.HighScore;
            TotalScore = record.TotalScore;
            Cp = record.Cp;
            Level = record.Level;
            Portrait = record.IconId;
        }
    }
}
