using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c;
using Nekoyume.Game;
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
            SetBoosted(
                BoostScheduleFrontHelper.FindActiveNow(BoostScheduleSheet.Targets.WorldBossBattleReward, row.BossId),
                row.BossId, row.Rank, row.RuneMin, row.RuneMax, row.Crystal, row.Circle);
        }

        public void Set(WorldBossKillRewardSheet.Row row)
        {
            SetBoosted(
                BoostScheduleFrontHelper.FindActiveNow(BoostScheduleSheet.Targets.WorldBossKillReward, row.BossId),
                row.BossId, row.Rank, row.RuneMin, row.RuneMax, row.Crystal, row.Circle);
        }

        /// <summary>
        /// 이벤트 중이면 액션과 같은 규칙(<c>WorldBossHelper.ApplyRewardBoost</c>, 뽑은 양에 적용)으로 키운 양을 보여준다.
        /// 룬은 뽑은 총량을 룬 종류별로 나눈 뒤 종류마다 적용한다 — MUL 은 각각 내림해 실제 합이 범위 끝값보다 조금 작을 수
        /// 있고, ADD 는 뽑힌 종류 수만큼 더해져 실제 합이 크다(<see cref="WorldBossFrontHelper.ApplyRuneRewardBoost(BoostScheduleSheet.Row,int,int,int,int)"/>).
        /// </summary>
        private void SetBoosted(
            BoostScheduleSheet.Row boost,
            int bossId,
            int rank,
            int runeMin,
            int runeMax,
            int crystal,
            int circle)
        {
            circle = WorldBossHelper.ApplyRewardBoost(boost, circle);
            var materials = new List<(int itemId, int quantity)>();
            if (circle > 0)
            {
                materials.Add((600402, circle));
            }

            SetRewardItem(
                WorldBossFrontHelper.ApplyRuneRewardBoost(boost, runeMin, runeMax, bossId, rank),
                WorldBossHelper.ApplyRewardBoost(boost, crystal),
                materials);
        }

        public void Set(WorldBossContributionRewardSheet.Row row)
        {
            if (_rankObject != null)
            {
                Destroy(_rankObject);
            }

            // Rank/ranking image are battle/grade-tab artifacts; season-pool variants
            // strip those GameObjects so the references can be unassigned in their prefab.
            if (rankText != null)
            {
                rankText.gameObject.SetActive(false);
            }
            if (rankingImageContainer != null)
            {
                rankingImageContainer.gameObject.SetActive(false);
            }
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

            SetRewardItem(((int)runeSum, (int)runeSum), crystal, materials);
        }

        private void SetRewardItem((int min, int max) rune, BigInteger crystal, List<(int itemId, int quantity)> materials)
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
                    if (TableSheets.Instance.ItemSheet.TryGetValue(itemId, out var row))
                    {
                        material.icon.sprite = SpriteHelper.GetItemIcon(itemId, row.ItemSubType, row.Grade);
                    }
                    else
                    {
                        material.icon.sprite = SpriteHelper.GetItemIcon(itemId);
                    }
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
