using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        private Button claimRewardButton;

        [SerializeField]
        private GameObject loadingIndicator;

        private readonly List<IDisposable> _disposables = new();

        private void Start()
        {
            claimRewardButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    ClaimSeasonReward();
                }).AddTo(gameObject);

            WorldBossStates.SubscribeGradeRewards(b => notification.SetActive(b));
            WorldBossStates.SubscribeReceivingSeasonRewards(UpdateIndicator);
            WorldBossStates.SubscribeCanReceivedSeasonRewards(UpdateClaimButton);
            WorldBossStates.SubscribeHasSeasonRewards(UpdateClaimButton);
            UpdateClaimButton();

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
            if (States.Instance.CurrentAvatarState == null || Game.Game.instance.Agent == null)
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

            if (WorldBossStates.GetPreRaiderState(avatarAddress) is null)
            {
                return;
            }

            if (!WorldBossStates.IsReceivingSeasonRewards(avatarAddress))
            {
                RequestManager.instance.IsExistSeasonReward(row.Id, avatarAddress);
            }

            UpdateIndicator();
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateClaimButton()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var canReceived = WorldBossStates.CanReceiveSeasonRewards(avatarAddress);
            if (!canReceived)
            {
                claimRewardButton.gameObject.SetActive(false);
                seasonRewardIcon.gameObject.SetActive(false);
                return;
            }

            var hasRewards = WorldBossStates.HasSeasonRewards(avatarAddress);
            claimRewardButton.gameObject.SetActive(hasRewards);
            seasonRewardIcon.gameObject.SetActive(hasRewards);
        }

        private void UpdateIndicator()
        {
            if (States.Instance.CurrentAvatarState == null)
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var isReceiving = WorldBossStates.IsReceivingSeasonRewards(avatarAddress);
            loadingIndicator.SetActive(isReceiving);
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
                    timeBlock.SetTimeBlock($"{row.EndedBlockIndex - currentBlockIndex:#,0}",
                        Util.GetBlockToTime(row.EndedBlockIndex - currentBlockIndex));

                    var avatarAddress = States.Instance.CurrentAvatarState.address;
                    var raiderState = WorldBossStates.GetRaiderState(avatarAddress);
                    if (raiderState is null)
                    {
                        ticketContainer.SetActive(false);
                    }
                    else
                    {
                        ticketContainer.SetActive(true);
                        var count =
                            WorldBossFrontHelper.GetRemainTicket(raiderState, currentBlockIndex);
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
