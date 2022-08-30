using System.Linq;
using Nekoyume.Helper;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossBattleRewardItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI rune;

        [SerializeField]
        private TextMeshProUGUI crystal;

        [SerializeField]
        private TextMeshProUGUI rankText;

        [SerializeField]
        private Image rankingIcon;

        [SerializeField]
        private GameObject rankTextContainer;

        [SerializeField]
        private GameObject rankingIconContainer;

        [SerializeField]
        private GameObject selected;

        public void Reset()
        {
            selected.SetActive(false);
        }

        public void Set(WorldBossBattleRewardSheet.Row row)
        {
            rune.text = $"{row.RuneMin:#,0}~{row.RuneMax:#,0}";
            crystal.text = $"{row.Crystal:#,0}";
        }

        public void Set(WorldBossKillRewardSheet.Row row)
        {
            rune.text = $"{row.RuneMin:#,0}~{row.RuneMax:#,0}";
            crystal.text = $"{row.Crystal:#,0}";
        }

        public void Set(WorldBossRankingRewardSheet.Row row, int myRank, int userCount)
        {
            rankTextContainer.gameObject.SetActive(false);
            rankingIconContainer.gameObject.SetActive(false);
            switch (row.RankingMin)
            {
                case 1:
                case 2:
                case 3:
                    rankTextContainer.gameObject.SetActive(true);
                    rankingIcon.sprite = SpriteHelper.GetRankIcon(row.RankingMin);
                    selected.SetActive(row.RankingMin == myRank);
                    break;
                default:
                    rankingIconContainer.gameObject.SetActive(true);
                    if (row.RankingMin == 0)
                    {
                        rankText.text = row.RateMin > 1
                            ? $"{row.RateMin}%~{row.RateMax}%"
                            : $"{row.RateMax}%";

                        var rate = userCount > 0 ? (int)(((float)myRank / userCount) * 100) : 0;
                        var value = myRank > 100 && (row.RateMin <= rate && rate <= row.RateMax);
                        selected.SetActive(value);
                    }
                    else
                    {
                        rankText.text = $"{row.RankingMin}~{row.RankingMax}";
                        var value = row.RankingMin <= myRank && myRank <= row.RankingMax;
                        selected.SetActive(value);
                    }
                    break;
            }

            var runeSum = row.Runes.Sum(x => x.RuneQty);
            rune.text = $"{runeSum:#,0}";
            crystal.text = $"{row.Crystal:#,0}";
        }
    }
}
