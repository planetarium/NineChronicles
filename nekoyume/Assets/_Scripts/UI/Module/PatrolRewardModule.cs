using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
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

        public void OnChangeRewards(Dictionary<PatrolRewardType, int> rewards)
        {
            foreach (var rewardView in rewardViews)
            {
                rewardView.cumulativeReward.container.SetActive(false);
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
    }
}
