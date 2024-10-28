using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.ApiClient;
using Nekoyume.L10n;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class PatrolRewardModule : MonoBehaviour
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

        [SerializeField] private RewardData[] rewardViews;
        [SerializeField] private TextMeshProUGUI patrolTimeText;
        [SerializeField] private Image patrolTimeGauge;
        [SerializeField] private TextMeshProUGUI gaugeUnitText1;
        [SerializeField] private TextMeshProUGUI gaugeUnitText2;

        public readonly Subject<TimeSpan> RemainTime = new ();

        public void Awake()
        {
            PatrolReward.RewardModels
                .Where(model => model != null)
                .Subscribe(SetRewardModels)
                .AddTo(gameObject);

            // it must be called after Init PatrolReward.PatrolTime (first called avatar selected)
            PatrolReward.PatrolTime
                .Where(_ => gameObject.activeSelf)
                .Subscribe(time => SetPatrolTime(time, PatrolReward.Interval))
                .AddTo(gameObject);

            // Todo: receiveButton을 밖에서 정상적으로 받아서 쓸 수 있도록 방법을 찾아보자
            PatrolReward.Claiming.Where(claiming => claiming)
                .Subscribe()
                // .Subscribe(_ => receiveButton.Interactable = false)
                .AddTo(gameObject);
        }

        public async void SetData()
        {
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var level = Game.Game.instance.States.CurrentAvatarState.level;
            if (PatrolReward.NeedToInitialize(avatarAddress))
            {
                await PatrolReward.InitializeInformation(avatarAddress.ToHex(),
                    agentAddress.ToHex(), level);
            }
            else if (PatrolReward.NextLevel <= level)
            {
                await PatrolReward.LoadPolicyInfo(level);
            }

            SetIntervalText(PatrolReward.Interval);
            // SetPatrolTime(PatrolReward.PatrolTime.Value); // 구독하고 있으니 필요 없지 않나?
        }

        #region UI

        // RewardModels
        private void SetRewardModels(List<PatrolRewardModel> rewardModels)
        {
            foreach (var rewardView in rewardViews)
            {
                rewardView.cumulativeReward.container.SetActive(false);
            }

            foreach (var model in rewardModels)
            {
                var rewardType = GetRewardType(model);
                var value = model.PerInterval;
                var view = rewardViews.FirstOrDefault(view => view.rewardType == rewardType);
                if (view == null)
                {
                    continue;
                }

                view.cumulativeReward.container.SetActive(value > 0);
                view.cumulativeReward.text.text = $"{value}";
            }
        }

        // Time
        private void SetPatrolTime(TimeSpan patrolTime, TimeSpan interval)
        {
            patrolTimeText.text =
                L10nManager.Localize("UI_PATROL_TIME_FORMAT", TimeSpanToString(patrolTime));
            patrolTimeGauge.fillAmount = (float)(patrolTime / interval);

            var patrolTimeWithOutSeconds =
                new TimeSpan(patrolTime.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
            var remainTime = interval - patrolTimeWithOutSeconds;
            RemainTime.OnNext(remainTime);
        }

        private void SetIntervalText(TimeSpan interval)
        {
            gaugeUnitText1.text = TimeSpanToString(interval / 2);
            gaugeUnitText2.text = TimeSpanToString(interval);
        }

        public static string TimeSpanToString(TimeSpan time)
        {
            var hourExist = time.TotalHours >= 1;
            var minuteExist = time.Minutes >= 1;
            var hourText = hourExist ? $"{(int)time.TotalHours}h " : string.Empty;
            var minuteText = minuteExist || !hourExist ? $"{time.Minutes}m" : string.Empty;
            return $"{hourText}{minuteText}";
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

        #endregion
    }
}
