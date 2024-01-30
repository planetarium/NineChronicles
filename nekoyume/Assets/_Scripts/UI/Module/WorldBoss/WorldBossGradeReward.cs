using System.Collections.Generic;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
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
        private List<WorldBossGradeRewardItem> items;

        [SerializeField]
        private ConditionalButton claimButton;

        [SerializeField]
        private Image startGaugeImage;

        [SerializeField]
        private Image middleGaugeImage;

        [SerializeField]
        private GameObject loadingIndicator;

        private bool _canReceive;
        private RaiderState _raiderState;
        private int _raidId;

        private void Start()
        {
            claimButton.OnSubmitSubject
                .Subscribe(_ => ClaimRaidReward())
                .AddTo(gameObject);

            claimButton.OnClickDisabledSubject
                .Subscribe(_ => { }).AddTo(gameObject);

            WorldBossStates.SubscribeReceivingGradeRewards((b) =>
            {
                loadingIndicator.SetActive(b);
                if (b)
                {
                    Set(_raiderState, _raidId);
                }

                UpdateClaimButton();
            });
        }

        private void OnEnable()
        {
            UpdateClaimButton();
        }

        public override void Reset()
        {
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

            if (!Game.Game.instance.TableSheets.WorldBossCharacterSheet
                    .TryGetValue(row.BossId, out var characterRow))
            {
                return;
            }

            _raiderState = raiderState;
            _raidId = raidId;

            Widget.Find<WorldBossRewardScreen>().CachingInformation(raiderState, row.BossId);
            var highScore = raiderState?.HighScore ?? 0;
            var currentRank = WorldBossHelper.CalculateRank(characterRow, highScore);
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var latestRewardRank = WorldBossStates.IsReceivingGradeRewards(avatarAddress)
                ? currentRank
                : raiderState?.LatestRewardRank ?? 0;

            _canReceive = latestRewardRank < currentRank;
            UpdateItems(characterRow, rows, latestRewardRank, currentRank);
            UpdateGauges(characterRow, highScore, currentRank);
            UpdateRecord(highScore);
        }

        private void ClaimRaidReward()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            WorldBossStates.SetReceivingGradeRewards(avatarAddress, true);
            WorldBossStates.SetHasGradeRewards(avatarAddress, false);
            ActionManager.Instance.ClaimRaidReward().Subscribe();
            _canReceive = false;
            UpdateClaimButton();
        }

        private void UpdateItems(
            WorldBossCharacterSheet.Row bossRow,
            IReadOnlyList<WorldBossRankRewardSheet.Row> rows,
            int latestRewardRank,
            int currentRank)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var score = WorldBossFrontHelper.GetScoreInRank(i + 1, bossRow);
                items[i].Set(score, rows[i].Rune, rows[i].Crystal);

                if (i + 1 <= latestRewardRank)
                {
                    items[i].SetStatus(WorldBossGradeRewardItem.Status.Received);
                }
                else
                {
                    items[i].SetStatus(i < currentRank
                        ? WorldBossGradeRewardItem.Status.Active
                        : WorldBossGradeRewardItem.Status.Normal);
                }
            }
        }

        private void UpdateGauges(WorldBossCharacterSheet.Row bossRow, long highScore,
            int currentRank)
        {
            switch (currentRank)
            {
                case (int)WorldBossGrade.None:
                    var d = WorldBossFrontHelper.GetScoreInRank((int)WorldBossGrade.D, bossRow);
                    startGaugeImage.fillAmount = d > 0 ? highScore / (float)d : 0;
                    middleGaugeImage.fillAmount = 0;
                    break;
                case (int)WorldBossGrade.S:
                    startGaugeImage.fillAmount = 1;
                    middleGaugeImage.fillAmount = 1;
                    break;
                default:
                    var curRankScore = WorldBossFrontHelper.GetScoreInRank(currentRank, bossRow);
                    var nextRankScore =
                        WorldBossFrontHelper.GetScoreInRank(currentRank + 1, bossRow);
                    var max = nextRankScore - curRankScore;
                    var cur = highScore - curRankScore;
                    startGaugeImage.fillAmount = 1;
                    middleGaugeImage.fillAmount =
                        (0.25f * (currentRank - 1)) + (0.25f * (cur / (float)max));
                    break;
            }
        }

        private void UpdateRecord(long highScore)
        {
            myBestRecordText.text = $"{highScore:#,0}";
        }

        private void UpdateClaimButton()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            claimButton.Interactable = _canReceive;
            claimButton.Text = WorldBossStates.IsReceivingGradeRewards(avatarAddress)
                ? string.Empty
                : L10nManager.Localize("UI_CLAIM_REWARD");
        }
    }
}
