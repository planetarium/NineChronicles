using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class WorldBossMenu : MainMenu
    {
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
        private RectTransform onSeasonOutlineRect;

        [SerializeField]
        private RectTransform offSeasonOutlineRect;

        private readonly List<IDisposable> _disposables = new();

        private const long SettleSeasonRewardInterval = 7200;

        private bool _hasGradeRewards;
        private WorldBossState _worldBossState;

        public bool HasGradeRewards
        {
            set
            {
                _hasGradeRewards = value;
                SetRedDot();
            }
        }

        public WorldBossState WorldBossState
        {
            set
            {
                _worldBossState = value;
                SetRedDot();
            }
        }

        private void Start()
        {
            WorldBossStates.SubscribeGradeRewards(b => HasGradeRewards = b);
            WorldBossStates.SubscribeWorldBossState(state => WorldBossState = state);

            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateBlockIndex).AddTo(_disposables);
            UpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
            CheckSeasonRewards();
        }

        private void OnEnable()
        {
            UpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
            CheckSeasonRewards();
        }

        private void SetRedDot()
        {
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var isOnSeason = WorldBossStates.IsOnSeason;
            var preRaiderState = WorldBossStates.GetPreRaiderState(avatarAddress);

            if (preRaiderState is null)
            {
                notification.SetActive(false);
                return;
            }

            notification.SetActive(_hasGradeRewards || (!isOnSeason && !preRaiderState.HasClaimedReward));
        }

        private void CheckSeasonRewards()
        {
            if (States.Instance.CurrentAvatarState == null)
            {
                return;
            }

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
                // TODO: Set tutorial target to season reward icon
                seasonRewardIcon.gameObject.SetActive(true);
                return;
            }

            seasonRewardIcon.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
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
                    Game.Game.instance.Stage?.TutorialController?.SetTutorialTarget(new TutorialTarget
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
