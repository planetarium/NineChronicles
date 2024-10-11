using System;
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
        [Serializable]
        private class RewardItem
        {
            public GameObject container;
            public Image icon;
            public TextMeshProUGUI text;
        }

        [SerializeField]
        private RewardItem runeItem;

        [SerializeField]
        private RewardItem crystalItem;

        [SerializeField]
        private RewardItem[] materialItems;

        [SerializeField]
        private TextMeshProUGUI rankText;

        [SerializeField]
        private GameObject rankingImageContainer;

        [SerializeField]
        private GameObject selected;

        private GameObject _rankObject;

        public void Reset()
        {
            selected.SetActive(false);
        }

        public void Set(WorldBossBattleRewardSheet.Row row)
        {
            SetRewardItem((row.RuneMin, row.RuneMax), row.Crystal, new[] { (600402, row.Circle) });
        }

        public void Set(WorldBossKillRewardSheet.Row row)
        {
            SetRewardItem((row.RuneMin, row.RuneMax), row.Crystal, new[] { (600402, row.Circle) });
        }

        public void Set(WorldBossRankingRewardSheet.Row row, int myRank, int userCount)
        {
            if (_rankObject != null)
            {
                Destroy(_rankObject);
            }

            rankText.gameObject.SetActive(false);
            rankingImageContainer.gameObject.SetActive(false);
            if (row.RankingMin != 0 && row.RankingMax != 0)
            {
                if (row.RankingMin == row.RankingMax)
                {
                    rankingImageContainer.gameObject.SetActive(true);
                    _rankObject = Instantiate(
                        WorldBossFrontHelper.GetRankPrefab(row.RankingMin),
                        rankingImageContainer.transform);
                    selected.SetActive(row.RankingMin == myRank);
                }
                else
                {
                    rankText.gameObject.SetActive(true);
                    rankText.text = $"{row.RankingMin}~{row.RankingMax}";
                    var value = row.RankingMin <= myRank && myRank <= row.RankingMax;
                    selected.SetActive(value);
                }
            }
            else
            {
                rankText.gameObject.SetActive(true);
                rankText.text = row.RateMin > 1
                    ? $"{row.RateMin}%~{row.RateMax}%"
                    : $"{row.RateMax}%";

                var rate = userCount > 0 ? (int)((float)myRank / userCount * 100) : 0;
                var value = myRank > 100 && row.RateMin <= rate && rate <= row.RateMax;
                selected.SetActive(value);
            }

            var runeSum = row.Runes.Sum(x => x.RuneQty);
            SetRewardItem((runeSum, runeSum), row.Crystal, row.Materials.ToArray());
        }

        private void SetRewardItem(
            (int min, int max) rune,
            int crystal,
            (int itemId, int quantity)[] materials)
        {
            runeItem.container.gameObject.SetActive(rune.max > 0);
            runeItem.text.text = rune.min == rune.max
                ? $"{rune.max:#,0}"
                : $"{rune.min:#,0}~{rune.max}";

            crystalItem.container.gameObject.SetActive(crystal > 0);
            crystalItem.text.text = $"{crystal:#,0}";

            for (var i = 0; i < materialItems.Length; i++)
            {
                var material = materialItems[i];
                if (i < materials.Length)
                {
                    var (itemId, quantity) = materials[i];
                    material.container.gameObject.SetActive(true);
                    material.icon.sprite = SpriteHelper.GetItemIcon(itemId);
                    material.text.text = $"{quantity:#,0}";
                }
                else
                {
                    material.container.gameObject.SetActive(false);
                }
            }
        }
    }
}
