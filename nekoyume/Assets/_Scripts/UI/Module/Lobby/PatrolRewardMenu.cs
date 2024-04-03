using System;
using System.Collections.Generic;
using Nekoyume.L10n;
using TMPro;
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

        [SerializeField]
        private GameObject patrollingObject;

        [SerializeField]
        private GameObject claimingObject;

        [SerializeField]
        private TextMeshProUGUI dimmedText;

        [SerializeField]
        private Sprite animationSprite;

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            if (!Widget.TryFind<PatrolRewardPopup>(out var popup))
            {
                return;
            }

            if (!popup.PatrolReward.Initialized)
            {
                NcDebug.LogError("PatrolReward is not initialized.");
                return;
            }

            popup.PatrolReward.PatrolTime
                .Select(time => time < popup.PatrolReward.Interval)
                .Where(_ => !popup.Claiming.Value)
                .Subscribe(patrolling => SetCanClaim(patrolling, false))
                .AddTo(_disposables);

            popup.Claiming.Where(claiming => claiming)
                .Subscribe(value => SetCanClaim(false, true)).AddTo(_disposables);
        }

        private void SetCanClaim(bool patrolling, bool claiming)
        {
            var canClaim = !(patrolling || claiming);
            notification.SetActive(canClaim);
            effect03.SetActive(canClaim);
            dimmedObjects.SetActive(!canClaim);

            patrollingObject.SetActive(patrolling);
            claimingObject.SetActive(claiming);
            var messageKey = claiming ? "UI_CLAIMING_REWARD" : "UI_PATROLLING";
            dimmedText.text = L10nManager.Localize(messageKey);
        }

        public void ClaimRewardAnimation()
        {
            var target = Widget.Find<HeaderMenuStatic>()
                .GetToggle(HeaderMenuStatic.ToggleType.AvatarInfo);
            var targetPosition = target ? target.position : Vector3.zero;

            ItemMoveAnimation.Show(
                animationSprite,
                rewardIcon.transform.position,
                targetPosition,
                Vector2.one * 1.5f,
                false,
                false,
                1f,
                0,
                ItemMoveAnimation.EndPoint.Inventory);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
