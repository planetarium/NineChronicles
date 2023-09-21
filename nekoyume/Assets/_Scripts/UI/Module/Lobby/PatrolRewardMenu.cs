using System;
using System.Collections.Generic;
using Nekoyume.UI.Model.Patrol;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;
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

        [SerializeField]
        private GameObject dimmedObjects;

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            if (!Widget.TryFind<PatrolRewardPopup>(out var popup))
            {
                return;
            }

            SetData(popup.PatrolReward);
        }

        private async void SetData(PatrolReward patrolReward)
        {
            if (!patrolReward.Initialized)
            {
                await patrolReward.Initialize();
            }

            patrolReward.CanClaim.Subscribe(SetCanClaim).AddTo(_disposables);
        }

        private void SetCanClaim(bool canClaim)
        {
            notification.SetActive(canClaim);
            effect03.SetActive(canClaim);
            dimmedObjects.SetActive(!canClaim);
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
