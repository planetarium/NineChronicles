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

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            var rewardLevel = 0; // Todo : Fill this
            var canGetReward = true; // Todo : Fill this

            rewardIcon.sprite = patrolRewardData.GetIcon(rewardLevel);
            notification.SetActive(canGetReward);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
