using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class WorldBossMenu : MainMenu
    {
        private const float RetryTime = 20f;
        private const int MaxRetryCount = 8;

        [SerializeField]
        private GameObject onSeason;

        [SerializeField]
        private GameObject offSeason;

        [SerializeField]
        private GameObject ticketContainer;

        [SerializeField]
        private GameObject timeContainer;

        [SerializeField]
        private GameObject notification;

        [SerializeField]
        private GameObject seasonRewardIcon;

        [SerializeField]
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private TimeBlock timeBlock;

        [SerializeField]
        private GameObject loadingIndicator;

        [SerializeField]
        private RectTransform onSeasonOutlineRect;

        [SerializeField]
        private RectTransform offSeasonOutlineRect;

        private readonly List<IDisposable> _disposables = new();
        private bool _madeRaidMail;

        private const long SettleSeasonRewardInterval = 7200;

        private void Start()
        {
            WorldBossStates.SubscribeGradeRewards(b => notification.SetActive(b));

            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateBlockIndex).AddTo(_disposables);
            UpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
            CheckSeasonRewards();
        }

        private void OnEnable()
        {
            UpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
            CheckSeasonRewards();
        }

        private void CheckSeasonRewards()
        {
            if (States.Instance.CurrentAvatarState == null)
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            if (WorldBossFrontHelper.IsItInSeason(blockIndex))
            {
                return;
            }

            if (!WorldBossFrontHelper.TryGetPreviousRow(blockIndex, out var row))
            {
                return;
            }

            if (blockIndex <= row.EndedBlockIndex + SettleSeasonRewardInterval)
            {
                loadingIndicator.SetActive(true);
                seasonRewardIcon.gameObject.SetActive(true);
                return;
            }

            if (WorldBossStates.GetPreRaiderState(avatarAddress) is null)
            {
                return;
            }

            loadingIndicator.SetActive(false);
            seasonRewardIcon.gameObject.SetActive(false);
            AddSeasonRewardMail(row.Id);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private void MakeSeasonRewardMail(int id, Address address, IEnumerable<SeasonRewards> rewards, bool isNew)
        {
            var now = Game.Game.instance.Agent.BlockIndex;
            foreach (var reward in rewards.OrderBy(reward => reward.ticker))
            {
                var currencyName = L10nManager.LocalizeCurrencyName(reward.ticker);
                LocalMailHelper.Instance.Add(
                    address,
                    new RaidRewardMail(
                        now,
                        Guid.NewGuid(),
                        now,
                        currencyName,
                        reward.amount,
                        id) {New = isNew}
                );
            }
        }

        private void AddSeasonRewardMail(int raidId)
        {
            const string success = "SUCCESS";
            const string cachingKey = "SeasonRewards_{0}_{1}_{2}";
            if (States.Instance.CurrentAvatarState == null || _madeRaidMail)
            {
                return;
            }

            _madeRaidMail = true;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var currentPlanetId = Game.Game.instance.CurrentPlanetId;
            var localRewardKey = string.Format(cachingKey, raidId, avatarAddress, currentPlanetId);

            var receivedRewardTxs = new List<string>();
            if (PlayerPrefs.HasKey(localRewardKey))
            {
                var json = PlayerPrefs.GetString(localRewardKey);
                var seasonReward = JsonUtility.FromJson<SeasonRewardRecord>(json);
                var receivedRewards = seasonReward.rewards
                    .Where(reward => reward.tx_result == success)
                    .ToArray();
                receivedRewardTxs.AddRange(receivedRewards.Select(reward => reward.tx_id));
                MakeSeasonRewardMail(
                    raidId,
                    new Address(seasonReward.avatarAddress),
                    receivedRewards,
                    false);

                // All tx(s) are SUCCESS, do not request.
                if (seasonReward.rewards.Length == receivedRewards.Length)
                {
                    return;
                }
            }

            // If have not cached data or have missing reward, Request SeasonRewards.
            StartCoroutine(WorldBossQuery.CoGetSeasonRewards(
                raidId,
                avatarAddress,
                json =>
                {
                    var seasonReward = JsonUtility.FromJson<SeasonRewardRecord>(json);
                    // only succeed and not cached tx makes mail.
                    var rewards = seasonReward.rewards.Where(reward =>
                        reward.tx_result == success &&
                        !receivedRewardTxs.Contains(reward.tx_id));
                    MakeSeasonRewardMail(
                        seasonReward.raidId,
                        new Address(seasonReward.avatarAddress),
                        rewards,
                        true);
                    PlayerPrefs.SetString(localRewardKey, json);
                }, null));

            // Delete old-cached data.
            var oldSeasonCachingKey = string.Format(cachingKey, raidId - 1, avatarAddress, currentPlanetId);
            if (PlayerPrefs.HasKey(oldSeasonCachingKey))
            {
                PlayerPrefs.DeleteKey(oldSeasonCachingKey);
            }
        }

        private void UpdateBlockIndex(long currentBlockIndex)
        {
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            switch (curStatus)
            {
                case WorldBossStatus.OffSeason:
                    onSeason.SetActive(false);
                    offSeason.SetActive(true);
                    timeContainer.SetActive(false);
                    ticketContainer.SetActive(false);
                    // Set tutorial target to off-season object
                    Game.Game.instance.Stage.TutorialController.SetTutorialTarget(new TutorialTarget
                    {
                        type = TutorialTargetType.WorldBossButton,
                        rectTransform = offSeasonOutlineRect
                    });
                    break;
                case WorldBossStatus.Season:
                    if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
                    {
                        return;
                    }

                    if (States.Instance.CurrentAvatarState is null)
                    {
                        return;
                    }

                    onSeason.SetActive(true);
                    offSeason.SetActive(false);
                    timeContainer.SetActive(true);
                    // Set tutorial target to on-season object
                    Game.Game.instance.Stage.TutorialController.SetTutorialTarget(new TutorialTarget
                    {
                        type = TutorialTargetType.WorldBossButton,
                        rectTransform = onSeasonOutlineRect
                    });
                    var remainTime = row.EndedBlockIndex - currentBlockIndex;
                    timeBlock.SetTimeBlock($"{remainTime:#,0}", remainTime.BlockRangeToTimeSpanString());

                    var avatarAddress = States.Instance.CurrentAvatarState.address;
                    var raiderState = WorldBossStates.GetRaiderState(avatarAddress);
                    if (raiderState is null)
                    {
                        ticketContainer.SetActive(false);
                    }
                    else
                    {
                        ticketContainer.SetActive(true);
                        var count = WorldBossFrontHelper.GetRemainTicket(
                            raiderState,
                            currentBlockIndex,
                            States.Instance.GameConfigState.DailyWorldBossInterval);
                        ticketText.text = $"{count}";
                    }

                    break;
                case WorldBossStatus.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
