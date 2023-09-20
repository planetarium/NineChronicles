using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class PatrolRewardPopup : PopupWidget
    {
        [Serializable]
        private class RewardView
        {
            public GameObject container;
            public TextMeshProUGUI text;
        }

        [Serializable]
        private class RewardData
        {
            public PatrolRewardType rewardType;
            public RewardView cumulativeReward;
            public RewardView rewardPerTime;
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private RewardData[] rewardViews;
        [SerializeField] private TextMeshProUGUI patrolTimeText;
        [SerializeField] private Image patrolTimeGauge;
        [SerializeField] private ConditionalButton receiveButton;

        private static readonly TimeSpan MaxTime = new(24, 0, 0);
        private static readonly TimeSpan MinTime = new(0, 10, 0);
        private readonly Dictionary<PatrolRewardType, int> _rewards = new();
        private readonly List<IDisposable> _disposables = new();

        private TimeSpan PatrolTime
        {
            get
            {
                var patrolTime = DateTime.Now - SharedModel.LastRewardTime.Value;
                return patrolTime > MaxTime ? MaxTime : patrolTime;
            }
        }

        public bool Notification => PatrolTime >= MinTime;

        public readonly PatrolReward SharedModel = new();

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
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            if (!SharedModel.Initialized)
            {
                SharedModel.Initialize();
            }

            if (!_initialized)
            {
                Init();
            }

            OnChangeTime(PatrolTime);

            var level = Game.Game.instance.States.CurrentAvatarState.level;
            if (SharedModel.NextLevel <= level)
            {
                // PatrolReward.LoadPolicyInfo
                SharedModel.LoadPolicyInfo(level);
            }
        }

        private void Init()
        {
            _disposables.DisposeAllAndClear();

            SharedModel.RewardModels
                .Subscribe(OnChangeRewardModels)
                .AddTo(_disposables);

            SharedModel.LastRewardTime
                .Subscribe(_ => OnChangeTime(PatrolTime))
                .AddTo(_disposables);

            Observable
                .Timer(TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(1))
                .Where(_ => gameObject.activeSelf && SharedModel.Initialized)
                .Subscribe(_ => OnChangeTime(PatrolTime))
                .AddTo(_disposables);

            receiveButton.OnSubmitSubject
                .Where(_ => SharedModel.Initialized)
                .Subscribe(_ => ClaimReward(_rewards))
                .AddTo(_disposables);

            _initialized = true;
        }

        // rewardModels
        private void OnChangeRewardModels(List<PatrolRewardModel> rewardModels)
        {
            foreach (var bonusRewardView in rewardViews)
            {
                bonusRewardView.rewardPerTime.container.SetActive(false);
            }

            foreach (var reward in rewardModels)
            {
                var rewardType = GetRewardType(reward);
                var view = rewardViews.FirstOrDefault(view => view.rewardType == rewardType);
                if (view == null)
                {
                    continue;
                }

                var interval = reward.RewardInterval;
                var intervalText = interval.TotalHours > 1
                    ? $"{interval.TotalHours}h"
                    : $"{interval.TotalMinutes}m";
                view.rewardPerTime.container.SetActive(true);
                view.rewardPerTime.text.text = $"{reward.PerInterval}/{intervalText}";
            }

            SetRewards(rewardModels, PatrolTime);
        }

        // time
        private void OnChangeTime(TimeSpan patrolTime)
        {
            var hourText = patrolTime.TotalHours >= 1
                ? $"{(int)patrolTime.TotalHours}h "
                : string.Empty;
            var minuteText = patrolTime.TotalHours < 1 || patrolTime.Minutes >= 1
                ? $"{patrolTime.Minutes}m"
                : string.Empty;
            patrolTimeText.text = L10nManager.Localize(
                "UI_PATROL_TIME_FORMAT", $"{hourText}{minuteText}");
            patrolTimeGauge.fillAmount = (float)(patrolTime / MaxTime);

            var ticksWithoutSeconds = patrolTime.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute;
            var remainTime = MinTime - new TimeSpan(ticksWithoutSeconds);
            var canReceive = remainTime <= TimeSpan.Zero;
            receiveButton.Interactable = canReceive;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME", $"{remainTime.Minutes}m");

            SetRewards(SharedModel.RewardModels.Value, patrolTime);
        }

        private void OnChangeRewards(Dictionary<PatrolRewardType, int> rewards)
        {
            foreach (var bonusRewardView in rewardViews)
            {
                bonusRewardView.cumulativeReward.container.SetActive(false);
            }

            foreach (var (rewardType, value) in rewards)
            {
                var view = rewardViews.FirstOrDefault(view => view.rewardType == rewardType);
                if (view == null)
                {
                    continue;
                }

                view.cumulativeReward.container.SetActive(value > 0);
                view.cumulativeReward.text.text = $"{value}";
            }
        }

        private void SetRewards(List<PatrolRewardModel> rewardModels, TimeSpan patrolTime)
        {
            _rewards.Clear();

            foreach (var reward in rewardModels)
            {
                var rewardType = GetRewardType(reward);
                _rewards[rewardType] = reward.PerInterval * (int)(patrolTime / reward.RewardInterval);
            }

            OnChangeRewards(_rewards);
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
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            return reward.Currency switch
            {
                "CRYSTAL" => PatrolRewardType.Crystal,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void ClaimReward(Dictionary<PatrolRewardType, int> rewards)
        {
            foreach (var reward in rewards)
            {
                Debug.Log($"Reward : {reward.Key} x{reward.Value}");
            }

            Close();
            // PatrolRewardMenu.DailyRewardAnimation
            SharedModel.ClaimReward();
        }
    }
}
