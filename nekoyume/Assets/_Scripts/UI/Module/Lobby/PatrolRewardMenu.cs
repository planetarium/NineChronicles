using System;
using System.Collections.Generic;
using Nekoyume.UI.Model.Patrol;
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
        private Sprite animationSprite;

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

            this.UpdateAsObservable()  // For Test
                .Where(_ => Input.GetKeyDown(KeyCode.W))
                .Subscribe(x =>
                {
                    var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
                    var agentAddress = Game.Game.instance.States.AgentState.address;
                    patrolReward.LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
                });
        }

        private void SetCanClaim(bool canClaim)
        {
            notification.SetActive(canClaim);
            effect03.SetActive(canClaim);
            dimmedObjects.SetActive(!canClaim);
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
