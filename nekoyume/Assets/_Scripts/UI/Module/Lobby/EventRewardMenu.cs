using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.ApiClient;
using Nekoyume.Game.LiveAsset;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class EventRewardMenu : MainMenu
    {
        [SerializeField]
        private GameObject notification;

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

        private void OnEnable()
        {
            WaitForLiveAssetManagerAndSetup().Forget();
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void OnLobbyEnter()
        {
            WaitForLiveAssetManagerAndSetup().Forget();
        }

        private async UniTask WaitForLiveAssetManagerAndSetup()
        {
            var liveAssetManager = LiveAssetManager.instance;

            // LiveAssetManager가 초기화될 때까지 대기
            if (!liveAssetManager.IsInitialized)
            {
                await UniTask.WaitUntil(() => liveAssetManager.IsInitialized);
            }

            // 초기화 후 notification 설정
            SetNotification();

            // EventRewardPopup에 PatrolReward 탭이 있는 경우 실시간 업데이트 구독 설정
            SetupPatrolRewardSubscriptions();
        }

        private void SetupPatrolRewardSubscriptions()
        {
            // 기존 구독 정리 (중복 구독 방지)
            _disposables.DisposeAllAndClear();

            // EventRewardPopup에 PatrolReward 탭이 있는 경우에만 구독 설정
            if (!HasPatrolRewardTab())
            {
                return;
            }

            var avatarState = Game.Game.instance.States.CurrentAvatarState;
            if (avatarState != null && PatrolReward.NeedToInitialize(avatarState.address))
            {
                var avatarAddress = avatarState.address;
                var level = avatarState.level;
                var lastClaimedBlockIndex = Nekoyume.State.ReactiveAvatarState.PatrolRewardClaimedBlockIndex;
                var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                PatrolReward.InitializeInformation(avatarAddress, level, lastClaimedBlockIndex, currentBlockIndex);
            }

            // BlockIndex 변경 시 업데이트
            Game.Game.instance.Agent.BlockIndexSubject
                .Where(_ => !PatrolReward.Claiming.Value)
                .Subscribe(_ => SetNotification())
                .AddTo(_disposables);

            // Claiming 상태 변경 시 업데이트
            PatrolReward.Claiming
                .Subscribe(_ => SetNotification())
                .AddTo(_disposables);
        }

        private void SetNotification()
        {
            var hasNotification = false;

            // EventRewardPopup을 읽지 않았는지 확인
            var popup = Widget.Find<EventRewardPopup>();
            if (popup != null && popup.HasUnread && popup.HasEvent)
            {
                hasNotification = true;
            }

            // EventRewardPopup에 PatrolReward 탭이 있고 보상을 받을 수 있는 경우
            if (!hasNotification && HasPatrolRewardTab())
            {
                // PatrolReward 초기화 확인
                var avatarState = Game.Game.instance.States.CurrentAvatarState;
                if (avatarState != null)
                {
                    if (PatrolReward.NeedToInitialize(avatarState.address))
                    {
                        var avatarAddress = avatarState.address;
                        var level = avatarState.level;
                        var lastClaimedBlockIndex = Nekoyume.State.ReactiveAvatarState.PatrolRewardClaimedBlockIndex;
                        var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
                        PatrolReward.InitializeInformation(avatarAddress, level, lastClaimedBlockIndex, currentBlockIndex);
                    }

                    if (PatrolReward.CanClaim)
                    {
                        hasNotification = true;
                    }
                }
            }

            notification.SetActive(hasNotification);
        }

        private bool HasPatrolRewardTab()
        {
            var liveAssetManager = LiveAssetManager.instance;
            if (!liveAssetManager.IsInitialized)
            {
                return false;
            }

            var eventRewardPopupData = liveAssetManager.EventRewardPopupData;
            if (!eventRewardPopupData.HasEvent)
            {
                return false;
            }

            return eventRewardPopupData.EventRewards?.Any(reward =>
                reward.ContentPresetType == EventRewardPopupData.ContentPresetType.PatrolReward) ?? false;
        }
    }
}
