using System;
using System.Collections.Generic;
using Nekoyume.ApiClient;
using Nekoyume.L10n;
using Nekoyume.State;
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

        protected override void Awake()
        {
            base.Awake();
            Game.Lobby.OnLobbyEnterEvent += OnLobbyEnter;
        }

        private void OnDestroy()
        {
            Game.Lobby.OnLobbyEnterEvent -= OnLobbyEnter;
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void OnLobbyEnter()
        {
            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            if (avatarState is null)
            {
                NcDebug.LogWarning($"[{nameof(PatrolRewardMenu)}] AvatarState is null.");
                return;
            }

            if (PatrolReward.NeedToInitialize(avatarState.address))
            {
                var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
                var level = Game.Game.instance.States.CurrentAvatarState.level;
                var lastClaimedBlockIndex = ReactiveAvatarState.PatrolRewardClaimedBlockIndex;
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                PatrolReward.InitializeInformation(avatarAddress, level, lastClaimedBlockIndex, currentBlockIndex);
            }

            PatrolReward.PatrolTime
                .Where(_ => !PatrolReward.Claiming.Value)
                .Select(time => time < PatrolReward.Interval)
                .Subscribe(patrolling => SetCanClaim(patrolling, false))
                .AddTo(_disposables);

            PatrolReward.Claiming
                .Where(claiming => claiming)
                .Subscribe(_ => SetCanClaim(false, true)).AddTo(_disposables);
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
    }
}
