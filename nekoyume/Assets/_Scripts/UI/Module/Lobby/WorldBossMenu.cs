using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
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

        private int _getRewardRetryCount = 0;
        private int _requestRetryCount = 0;
        private bool _isExistSeasonReward;

        private readonly List<IDisposable> _disposables = new();
        private readonly List<SeasonRewards> _seasonRewards = new();

        private void Awake()
        {
            claimRewardButton.OnClickAsObservable()
                .Subscribe(_ => ClaimSeasonReward()).AddTo(gameObject);

            WorldBossStates.SubscribeKillRewards((b) => notification.SetActive(b));
            WorldBossStates.SubscribeSeasonRewards(UpdateClaimButton);
        }

        private void Start()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateBlockIndex)
                .AddTo(_disposables);
            UpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
            _requestRetryCount = 0;
            InitButton();
        }

        private void OnEnable()
        {
            UpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private void InitButton()
        {
            if (_requestRetryCount > MaxRetryCount)
            {
                return;
            }
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            if (!WorldBossFrontHelper.TryGetPreviousRow(blockIndex, out var row))
            {
                return;
            }

            _requestRetryCount++;
            Debug.Log($"[InitButton] retry count :{_requestRetryCount}");
            StartCoroutine(WorldBossQuery.CoIsExistSeasonReward(row.Id, avatarAddress,
                (b) =>
                {
                    _isExistSeasonReward = b.Contains("true");
                    UpdateClaimButton(_isExistSeasonReward);
                },
                InitButton));
        }

        private void UpdateClaimButton(bool value)
        {
            if (!_isExistSeasonReward)
            {
                claimRewardButton.gameObject.SetActive(false);
                seasonRewardIcon.gameObject.SetActive(false);
                return;
            }

            claimRewardButton.gameObject.SetActive(value);
            seasonRewardIcon.gameObject.SetActive(value);
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

        private void ClaimSeasonReward()
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            if (!WorldBossFrontHelper.TryGetPreviousRow(blockIndex, out var row))
            {
                return;
            }

            Debug.Log("OnClick claim button");
            Debug.Log($"Agent : {agentAddress}");
            Debug.Log($"Avatar : {avatarAddress}");
            Debug.Log($"Raid Id : {row.Id}");
            Widget.Find<GrayLoadingScreen>().Show("UI_LOADING_REWARD", true, 0.7f);
            StartCoroutine(WorldBossQuery.CoClaimSeasonReward(
                row.Id,
                agentAddress,
                avatarAddress,
                GetReward,
                () =>
                {
                    Widget.Find<GrayLoadingScreen>().Close();
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("UI_BOSS_SEASON_REWARD_REQUEST_FAIL"),
                        NotificationCell.NotificationType.Alert);
                }));
        }

        private async void GetReward(string json)
        {
            var result = JsonUtility.FromJson<SeasonRewardRecord>(json);
            var rewards = result.rewards.ToList();

            _getRewardRetryCount = 0;
            _seasonRewards.Clear();
            _seasonRewards.AddRange(result.rewards);

            var received = await WorldBossQuery.CheckTxStatus(rewards);
            await foreach (var reward in received)
            {
                rewards.Remove(reward);
            }

            if (rewards.Any())
            {
                StartCoroutine(CoCheckTxAgain(rewards));
            }
            else
            {
                ShowRewardPopup();
            }
        }

        private async void ShowRewardPopup()
        {
            Debug.Log("[GetReward] ShowRewardPopup!");
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var agentAddress = States.Instance.AgentState.address;
            var crystal = await Game.Game.instance.Agent.GetBalanceAsync(agentAddress, CrystalCalculator.CRYSTAL);
            States.Instance.SetCrystalBalance(crystal);
            await WorldBossStates.Set(avatarAddress);
            Widget.Find<GrayLoadingScreen>().Close();
            Widget.Find<WorldBossRewardPopup>().Show(_seasonRewards);
            claimRewardButton.gameObject.SetActive(false);
            seasonRewardIcon.gameObject.SetActive(false);
        }

        private IEnumerator CoCheckTxAgain(List<SeasonRewards> rewards)
        {
            if (_getRewardRetryCount > MaxRetryCount)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_BOSS_SEASON_REWARD_TX_FAIL"),
                    NotificationCell.NotificationType.Alert);
                yield break;
            }

            yield return new WaitForSeconds(RetryTime);
            _getRewardRetryCount++;
            GetRewardAgain(rewards);
        }

        private async void GetRewardAgain(List<SeasonRewards> rewards)
        {
            var received = await WorldBossQuery.CheckTxStatus(rewards);
            await foreach (var reward in received)
            {
                rewards.Remove(reward);
            }

            if (rewards.Any())
            {
                StartCoroutine(CoCheckTxAgain(rewards));
            }
            else
            {
                ShowRewardPopup();
            }
        }
    }
}
