using System;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;

    public class WorldBossSeasonReward : WorldBossRewardItem
    {
        [SerializeField]
        private TMP_Text worldBossTotalDamageText;

        [SerializeField]
        private WorldBossBattleRewardItem rewardItem;

        [SerializeField]
        private TMP_Text userTotalDamageText;

        [SerializeField]
        private BaseItemView[] rewardItems;

        [SerializeField]
        private ConditionalButton claimButton;

        private void Awake()
        {
            claimButton.OnSubmitSubject
                .Subscribe(_ => OnClickClaimButton())
                .AddTo(gameObject);

            claimButton.OnClickDisabledSubject
                .Subscribe(_ => OnClickDisableClaimButton())
                .AddTo(gameObject);
        }

        public override void Reset()
        {
        }

        public void Set(WorldBossState worldBossState, RaiderState raider, int raidId, bool isOnSeason)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var raidRow))
            {
                return;
            }

            var canClaim = !isOnSeason && !raider.HasClaimedReward;
            claimButton.SetCondition(() => canClaim);
            claimButton.UpdateObjects();

            var tableSheets = Game.Game.instance.TableSheets;
            var rewardRow = tableSheets.WorldBossKillRewardSheet.Values.FirstOrDefault(r => r.BossId == raidRow.BossId);
            if (rewardRow == null)
            {
                NcDebug.LogError($"Not found WorldBossKillRewardSheet for bossId: {raidRow.BossId}");
                return;
            }

            var worldBossTotalDamage = worldBossState?.TotalDamage ?? 0;
            var userTotalDamage = raider?.TotalScore ?? 0;

            worldBossTotalDamageText.text = worldBossTotalDamage.ToString();
            float ratio = 0;
            if (worldBossTotalDamage > 0)
            {
                ratio = userTotalDamage / (float)worldBossTotalDamage;
            }

            userTotalDamageText.text = $"{userTotalDamage.ToString()} ({ratio:P2})";
            rewardItem.Set(rewardRow);

            var contributeSheet = tableSheets.WorldBossContributionRewardSheet;
            var contributeRow = contributeSheet.Values.FirstOrDefault(r => r.BossId == raidRow.BossId);
            if (contributeRow == null)
            {
                NcDebug.LogError($"Not found WorldBossContributionRewardSheet for bossId: {raidRow.BossId}");
                return;
            }
            rewardItem.Set(contributeRow);

            foreach (var rewardItemView in rewardItems)
            {
                rewardItemView.gameObject.SetActive(false);
            }

            if (userTotalDamage == 0)
            {
                return;
            }

            for (var i = 0; i < contributeRow.Rewards.Count; ++i)
            {
                var currentItem = contributeRow.Rewards[i];
                if (!string.IsNullOrEmpty(currentItem.Ticker))
                {
                    rewardItems[i].gameObject.SetActive(true);
                    rewardItems[i].ItemViewSetCurrencyData(currentItem.Ticker, (decimal)currentItem.Count * (decimal)ratio);
                }
                else if (currentItem.ItemId > 0)
                {
                    rewardItems[i].gameObject.SetActive(true);
                    rewardItems[i].ItemViewSetItemData(currentItem.ItemId, (int)((decimal)currentItem.Count * (decimal)ratio));
                }
            }
        }

        private void OnClickClaimButton()
        {
            ActionManager.Instance.ClaimWorldBossReward().Subscribe();
            claimButton.SetCondition(() => false);
            claimButton.UpdateObjects();
        }

        private void OnClickDisableClaimButton()
        {
            OneLineSystem.Push(
                MailType.System,
                L10nManager.Localize("UI_BOSS_SEASON_REWARD_CANNOT_CLAIM_INFO"),
                NotificationCell.NotificationType.Information);
        }
    }
}
