using System;
using System.Collections.Generic;
using Nekoyume.L10n;
using TMPro;
using UniRx.Triggers;
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

            SetData(popup);
        }

        private async void SetData(PatrolRewardPopup popup)
        {
            var patrolReward = popup.PatrolReward;

            await patrolReward.Initialize();

            patrolReward.PatrolTime
                .Select(time => time < patrolReward.Interval)
                .Where(_ => !popup.Claiming.Value)
                .Subscribe(patrolling => SetCanClaim(patrolling, false))
                .AddTo(_disposables);

            popup.Claiming.Subscribe(value =>
            {
                if (value)
                {
                    SetCanClaim(false, true);
                }
            }).AddTo(_disposables);

            this.UpdateAsObservable()  // For internal Test
                .Where(_ => Input.GetKeyDown(KeyCode.W))
                .Subscribe(x =>
                {
                    var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
                    var agentAddress = Game.Game.instance.States.AgentState.address;
                    patrolReward.LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
                });
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
