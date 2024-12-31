using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
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
            public Image icon;
            public TextMeshProUGUI text;
        }

        [SerializeField] private RewardView[] rewardViews;
        [SerializeField] private TextMeshProUGUI patrolTimeText;
        [SerializeField] private Image patrolTimeGauge;
        [SerializeField] private TextMeshProUGUI gaugeUnitText1;
        [SerializeField] private TextMeshProUGUI gaugeUnitText2;

        public void Awake()
        {
            SetData();

            PatrolReward.RewardModels
                .Where(model => model != null)
                .Subscribe(SetRewardModels)
                .AddTo(gameObject);

            PatrolReward.PatrolTime
                .Where(_ => gameObject.activeSelf)
                .Subscribe(time => SetPatrolTime(time, PatrolReward.Interval))
                .AddTo(gameObject);
        }

        public void SetData()
        {
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            var level = Game.Game.instance.States.CurrentAvatarState.level;
            var lastClaimedBlockIndex = ReactiveAvatarState.PatrolRewardClaimedBlockIndex;
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            if (PatrolReward.NeedToInitialize(avatarAddress))
            {
                PatrolReward.InitializeInformation(avatarAddress, level, lastClaimedBlockIndex, currentBlockIndex);
            }
            else if (PatrolReward.NextLevel <= level)
            {
                PatrolReward.LoadPolicyInfo(level, currentBlockIndex);
            }

            SetIntervalText(PatrolReward.Interval);
            // SetPatrolTime(PatrolReward.PatrolTime.Value, PatrolReward.Interval); // 구독하고 있으니 필요 없지 않나?
        }

        #region UI

        // RewardModels
        private void SetRewardModels(List<PatrolRewardModel> rewardModels)
        {
            for (var i = 0; i < rewardViews.Length; i++)
            {
                var view = rewardViews[i];
                if (i >= rewardModels.Count)
                {
                    view.container.SetActive(false);
                    continue;
                }

                var model = rewardModels[i];
                var value = model.PerInterval;
                view.container.SetActive(value > 0);
                view.icon.sprite = GetSprite(model);
                view.text.text = $"{value}";
            }
        }

        // Time
        private void SetPatrolTime(long patrolTime, long interval)
        {
            patrolTimeText.text =
                L10nManager.Localize("UI_PATROL_TIME_FORMAT", patrolTime.BlockToTimeSpan());
            patrolTimeGauge.fillAmount = (float)(patrolTime / interval);
        }

        private void SetIntervalText(long interval)
        {
            gaugeUnitText1.text = (interval / 2).BlockRangeToTimeSpanString();
            gaugeUnitText2.text = interval.BlockRangeToTimeSpanString();
        }

        public static string TimeSpanToString(TimeSpan time)
        {
            var hourExist = time.TotalHours >= 1;
            var minuteExist = time.Minutes >= 1;
            var hourText = hourExist ? $"{(int)time.TotalHours}h " : string.Empty;
            var minuteText = minuteExist || !hourExist ? $"{time.Minutes}m" : string.Empty;
            return $"{hourText}{minuteText}";
        }

        private static Sprite GetSprite(PatrolRewardModel reward)
        {
            if (reward.ItemId != null)
            {
                return SpriteHelper.GetItemIcon(reward.ItemId.Value);
            }

            if (!string.IsNullOrEmpty(reward.Currency))
            {
                return SpriteHelper.GetFavIcon(reward.Currency);
            }

            return null;
        }

        #endregion
    }
}
