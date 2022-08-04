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

        public void Set(WorldBossKillRewardSheet.Row row, bool isSelected)
        {
            rune.text = $"{row.RuneMin:#,0}~{row.RuneMax:#,0}";
            crystal.text = $"{row.Crystal:#,0}";
            selected.SetActive(isSelected);
        }

        public void Set(WorldBossRankingRewardSheet.Row row, bool isSelected)
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
                    break;
                default:
                    rankingIconContainer.gameObject.SetActive(true);
                    if (row.RankingMin == 0)
                    {
                        rankText.text = row.RateMin > 1
                            ? $"{row.RateMin}%~{row.RateMax}%"
                            : $"{row.RateMax}%";
                    }
                    else
                    {
                        rankText.text = $"{row.RankingMin}~{row.RankingMax}";
                    }
                    break;
            }

            var runeSum = row.Runes.Sum(x => x.RuneQty);
            rune.text = $"{runeSum:#,0}";
            crystal.text = $"{row.Crystal:#,0}";
            selected.SetActive(isSelected);
        }
    }
}
