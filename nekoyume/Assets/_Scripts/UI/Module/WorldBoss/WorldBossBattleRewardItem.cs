using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c;
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
            var materials = new List<(int itemId, int quantity)>();
            if (row.Circle > 0)
            {
                materials.Add((600402, row.Circle));
            }

            SetRewardItem((row.RuneMin, row.RuneMax), row.Crystal, materials);
        }

        public void Set(WorldBossKillRewardSheet.Row row)
        {
            var materials = new List<(int itemId, int quantity)>();
            if (row.Circle > 0)
            {
                materials.Add((600402, row.Circle));
            }

            SetRewardItem((row.RuneMin, row.RuneMax), row.Crystal, materials);
        }

        public void Set(WorldBossContributionRewardSheet.Row row)
        {
            if (_rankObject != null)
            {
                Destroy(_rankObject);
            }

            rankText.gameObject.SetActive(false);
            rankingImageContainer.gameObject.SetActive(false);
            selected.SetActive(false);

            BigInteger runeSum = 0;
            BigInteger crystal = 0;
            var materials = new List<(int itemId, int quantity)>();
            foreach (var rewardModel in row.Rewards)
            {
                if (Currencies.IsRuneTicker(rewardModel.Ticker))
                {
                    runeSum += rewardModel.Count;
                }

                if (Currencies.Crystal.Ticker == rewardModel.Ticker)
                {
                    crystal += rewardModel.Count;
                }

                if (rewardModel.ItemId != 0)
                {
                    materials.Add((rewardModel.ItemId, (int)rewardModel.Count));
                }
            }

            SetRewardItem(((int)runeSum, (int)runeSum), (long)crystal, materials);
        }

        private void SetRewardItem((int min, int max) rune, long crystal, List<(int itemId, int quantity)> materials)
        {
            runeItem.container.gameObject.SetActive(rune.max > 0);
            runeItem.text.text = rune.min == rune.max
                ? $"{rune.max:#,0}"
                : $"{rune.min:#,0}~{rune.max}";

            crystalItem.container.gameObject.SetActive(crystal > 0);
            crystalItem.text.text = crystal.ToCurrencyNotation();

            for (var i = 0; i < materialItems.Length; i++)
            {
                var material = materialItems[i];
                if (i < materials.Count)
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
