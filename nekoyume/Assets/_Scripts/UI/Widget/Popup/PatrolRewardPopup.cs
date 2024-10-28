using System;
using System.Collections.Generic;
using mixpanel;
using Nekoyume.ApiClient;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class PatrolRewardPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private PatrolRewardModule patrolRewardModule;
        [SerializeField] private ConditionalButton receiveButton;

        public readonly PatrolReward PatrolReward = new();
        private readonly Dictionary<PatrolRewardType, int> _rewards = new();

        private bool _initialized;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
            CloseWidget = () => Close(true);

            receiveButton.OnSubmitSubject
                .Subscribe(_ => ClaimReward())
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (PatrolReward.Claiming.Value)
            {
                return;
            }

            ShowAsync(ignoreShowAnimation);
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            var clientInitialized = ApiClients.Instance.PatrolRewardServiceClient.IsInitialized;
            if (!clientInitialized)
            {
                NcDebug.Log(
                    $"[{nameof(PatrolRewardPopup)}]PatrolRewardServiceClient is not initialized.");
                return;
            }

            Analyzer.Instance.Track("Unity/PatrolReward/Show Popup", new Dictionary<string, Value>
            {
                ["PatrolTime"] = PatrolReward.PatrolTime.Value
            });

            var evt = new AirbridgeEvent("PatrolReward_Show_Popup");
            evt.AddCustomAttribute("patrol-time", PatrolReward.PatrolTime.Value.ToString());
            AirbridgeUnity.TrackEvent(evt);

            if (!_initialized)
            {
                Init();
            }

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var level = Game.Game.instance.States.CurrentAvatarState.level;
            if (!PatrolReward.Initialized)
            {
                await PatrolReward.InitializeInformation(avatarAddress.ToHex(),
                    agentAddress.ToHex(), level);
            }
            else if (PatrolReward.NextLevel <= level)
            {
                await PatrolReward.LoadPolicyInfo(level);
            }

            SetIntervalText(PatrolReward.Interval);
            OnChangeTime(PatrolReward.PatrolTime.Value);
            base.Show(ignoreShowAnimation);
        }

        private void ClaimReward()
        {
            Analyzer.Instance.Track("Unity/PatrolReward/Request Claim Reward");

            var evt = new AirbridgeEvent("PatrolReward_Request_Claim_Reward");
            AirbridgeUnity.TrackEvent(evt);

            PatrolReward.ClaimReward(() => Find<LobbyMenu>().PatrolRewardMenu.ClaimRewardAnimation());
            Close();
        }

        // it must be called after PatrolReward.InitializeInformation (called avatar selected)
        private void Init()
        {
            PatrolReward.RewardModels
                .Subscribe(OnChangeRewardModels)
                .AddTo(gameObject);

            PatrolReward.PatrolTime
                .Where(_ => gameObject.activeSelf)
                .Subscribe(OnChangeTime)
                .AddTo(gameObject);

            PatrolReward.Claiming.Where(claiming => claiming)
                .Subscribe(_ => receiveButton.Interactable = false)
                .AddTo(gameObject);

            _initialized = true;
        }

        // rewardModels
        private void OnChangeRewardModels(List<PatrolRewardModel> rewardModels)
        {
            SetRewards(rewardModels);
        }

        // time
        private void OnChangeTime(TimeSpan patrolTime)
        {
            patrolTimeText.text =
                L10nManager.Localize("UI_PATROL_TIME_FORMAT", GetTimeString(patrolTime));
            patrolTimeGauge.fillAmount = (float)(patrolTime / PatrolReward.Interval);

            var patrolTimeWithOutSeconds =
                new TimeSpan(patrolTime.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
            var remainTime = PatrolReward.Interval - patrolTimeWithOutSeconds;
            var canReceive = remainTime <= TimeSpan.Zero;

            receiveButton.Interactable = canReceive && !PatrolReward.Claiming.Value;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME", GetTimeString(remainTime));
        }

        private void SetIntervalText(TimeSpan interval)
        {
            gaugeUnitText1.text = GetTimeString(interval / 2);
            gaugeUnitText2.text = GetTimeString(interval);
        }

        private static string GetTimeString(TimeSpan time)
        {
            var hourExist = time.TotalHours >= 1;
            var minuteExist = time.Minutes >= 1;
            var hourText = hourExist ? $"{(int)time.TotalHours}h " : string.Empty;
            var minuteText = minuteExist || !hourExist ? $"{time.Minutes}m" : string.Empty;
            return $"{hourText}{minuteText}";
        }

        private void SetRewards(List<PatrolRewardModel> rewardModels)
        {
            _rewards.Clear();

            foreach (var reward in rewardModels)
            {
                var rewardType = GetRewardType(reward);
                _rewards[rewardType] = reward.PerInterval;
            }

            patrolRewardModule.OnChangeRewards(_rewards);
        }

        private static PatrolRewardType GetRewardType(PatrolRewardModel reward)
        {
            if (reward.ItemId != null)
            {
                return reward.ItemId switch
                {
                    500000 => PatrolRewardType.ApStone,
                    600201 => PatrolRewardType.GoldPowder,
                    800201 => PatrolRewardType.SilverPowder,
                    400000 => PatrolRewardType.Hourglass,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            return reward.Currency switch
            {
                "CRYSTAL" => PatrolRewardType.Crystal,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickClaimPatrolRewardButton()
        {
            receiveButton.OnSubmitSubject.OnNext(default);
        }
    }
}
