using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;

    public class WorldBossGradeReward : WorldBossRewardItem
    {
        [SerializeField]
        private TextMeshProUGUI myBestRecordText;

        [SerializeField]
        private List<WorldBossRewardBattleGradeItem> items;

        [SerializeField]
        private ConditionalButton claimButton;

        [SerializeField]
        private Image startGaugeImage;

        [SerializeField]
        private Image middleGaugeImage;

        [SerializeField]
        private Image endGaugeImage;

        private void Start()
        {
            claimButton.OnSubmitSubject
                .Subscribe(_ => ClaimRaidReward())
                .AddTo(gameObject);

            claimButton.OnClickDisabledSubject
                .Subscribe(_ =>
                {
                    // 여기서 받을 것 없다는 노티 띄워줘야 하려나?
                }).AddTo(gameObject);
        }

        public void Set(RaiderState raiderState, int raidId)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var row))
            {
                return;
            }

            var rewardSheet = Game.Game.instance.TableSheets.WorldBossRankRewardSheet;
            var rows = rewardSheet.Values.Where(x => x.BossId.Equals(row.BossId)).ToList();
            if (!rows.Any())
            {
                return;
            }

            Widget.Find<WorldBossRewardPopup>().CachingInformation(raiderState, row.BossId);
            var latestRewardRank = raiderState?.LatestRewardRank ?? 0;
            var highScore = raiderState?.HighScore ?? 0;
            var currentRank = WorldBossHelper.CalculateRank(highScore);
            var maxRankCount = rows.Count;
            UpdateItems(rows, latestRewardRank, currentRank);
            UpdateGauges(currentRank, maxRankCount);
            UpdateRecord(highScore);

            claimButton.Interactable = latestRewardRank < currentRank;
        }

        private void ClaimRaidReward()
        {
            Widget.Find<GrayLoadingScreen>().Show("UI_LOADING_REWARD", true, 0.7f);
            ActionManager.Instance.ClaimRaidReward();
        }

        private void UpdateItems(
            IReadOnlyList<WorldBossRankRewardSheet.Row> rows,
            int latestRewardRank,
            int currentRank)
        {
            for (var i = 0; i < items.Count; i++)
            {
                // todo : score 구해줘야함
                var score = (i + 1) * 100000;
                items[i].Set(score, rows[i].Rune, rows[i].Crystal);

                if (i + 1 <= latestRewardRank)
                {
                    items[i].SetStatus(WorldBossRewardBattleGradeItem.Status.Received);
                }
                else
                {
                    items[i].SetStatus(i < currentRank
                        ? WorldBossRewardBattleGradeItem.Status.Active
                        : WorldBossRewardBattleGradeItem.Status.Normal);
                }
            }
        }

        private void UpdateGauges(int currentRank, int maxRankCount)
        {
            // todo : 스코어나오면 fillAmount normalize 해줘야함
            startGaugeImage.fillAmount = currentRank < 1 ? 0 : 1;
            middleGaugeImage.fillAmount = (0.25f * currentRank) - 0.25f;
            endGaugeImage.gameObject.SetActive(currentRank == maxRankCount);
        }

        private void UpdateRecord(int highScore)
        {
            myBestRecordText.text = $"{highScore:#,0}";
        }
    }
}
