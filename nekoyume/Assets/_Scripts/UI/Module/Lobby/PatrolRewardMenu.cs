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
            if (!Widget.TryFind<PatrolRewardPopup>(out var popup))
            {
                return;
            }

            if (!popup.SharedModel.Initialized)
            {
                popup.SharedModel.Initialize();
            }

            var ticks = (popup.SharedModel.CreatedTime - DateTime.Now)
                .Ticks % TimeSpan.TicksPerDay / (TimeSpan.TicksPerHour * 6);
            var rewardLevel = new TimeSpan(ticks).Hours;
            var canGetReward = popup.Notification;
            notification.SetActive(canGetReward);

            SetLevel(rewardLevel);
        }

        public void SetLevel(int rewardLevel)
        {
            rewardIcon.sprite = patrolRewardData.GetIcon(rewardLevel);
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
