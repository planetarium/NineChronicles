using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Lobby
{
    public class PatrolRewardMenu : MainMenu
    {
        [SerializeField]
        private GameObject notification;

        [SerializeField]
        private Image rewardIcon;

        [SerializeField]
        private PatrolRewardDataScriptableObject patrolRewardData;

        [SerializeField]
        private GameObject effect00;

        [SerializeField]
        private GameObject effect03;

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            var rewardLevel = 0; // Todo : Fill this
            var canGetReward = true; // Todo : Fill this

            SetLevel(rewardLevel, canGetReward);
        }

        public void SetLevel(int rewardLevel, bool canGetReward)
        {
            rewardIcon.sprite = patrolRewardData.GetIcon(rewardLevel);
            notification.SetActive(canGetReward);
            effect00.SetActive(rewardLevel < 3);
            effect03.SetActive(rewardLevel >= 3);
        }

        public void DailyRewardAnimation()
        {
            // Todo
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
