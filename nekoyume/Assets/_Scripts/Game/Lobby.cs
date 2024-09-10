#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Game.Character;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Nekoyume.Game
{
    public class Lobby : MonoBehaviour
    {
        [SerializeField]
        private LobbyCharacter character = null!;

        [SerializeField]
        private FriendCharacter friendCharacter = null!;

        public LobbyCharacter Character => character;
        public FriendCharacter FriendCharacter => friendCharacter;

        public readonly ISubject<Object> OnLobbyEnterEnd = new Subject<Object>();
        
        private static Action<bool>? _onLobbyEnterEnd;
        
        public static event Action<bool>? OnLobbyEnterEvent
        {
            add
            {
                _onLobbyEnterEnd -= value;
                _onLobbyEnterEnd += value;
            }
            remove => _onLobbyEnterEnd -= value;
        }
        
        public static void Enter(bool showScreen = false)
        {
            _onLobbyEnterEnd?.Invoke(showScreen);
        }
        
        private void Awake()
        {
            OnLobbyEnterEvent += OnLobbyEnter;
        }

        private void OnDestroy()
        {
            OnLobbyEnterEvent -= OnLobbyEnter;
            _onLobbyEnterEnd = null;
        }

        private void OnLobbyEnter(bool showScreen)
        {
            OnLobbyEnterAsync(showScreen).Forget();
        }

        private async UniTask OnLobbyEnterAsync(bool showScreen)
        {
            Widget.Find<HeaderMenuStatic>().Close(true);
            
            var avatarState = States.Instance.CurrentAvatarState;
            var (equipments, costumes) = States.Instance.GetEquippedItems(BattleType.Adventure);
            var onFinish = false;
            Character.Set(avatarState, equipments, costumes, () => onFinish = true);

            await UniTask.WaitUntil(() => onFinish);
            
            Character.EnterLobby();
            Widget.Find<Menu>().EnterLobby();
            ActionCamera.instance.SetPosition(0f, 0f);
            ActionCamera.instance.Idle();

            ActionRenderHandler.Instance.Pending = false;
            
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            Widget.Find<Status>().Show();
            Widget.Find<EventBanner>().Show();
            var headerMenu = Widget.Find<HeaderMenuStatic>();
            if (!headerMenu.isActiveAndEnabled)
            {
                headerMenu.Show();
            }

            OnLobbyPopup();
            OnLobbyEnterEnd.OnNext(this);

            // Clear Memory
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        private void OnLobbyPopup()
        {          
            const int requiredStage = LiveAsset.GameConfig.RequiredStage.ShowPopupLobbyEntering;
            if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage))
            {
                return;
            }
            
            try
            {
#if UNITY_ANDROID || UNITY_IOS
                    var shopListPopup = Widget.Find<ShopListPopup>();
                    if (shopListPopup.HasUnread)
                    {
                        shopListPopup.ShowAtLobbyEntering();
                    }
#endif
                var avatarInfo = ApiClients.Instance.SeasonPassServiceManager.AvatarInfo;
                if (avatarInfo.Value != null && Widget.TryFind<SeasonPassNewPopup>(out var seasonPassNewPopup))
                {
                    if (seasonPassNewPopup.HasUnread &&
                        avatarInfo.HasValue &&
                        !avatarInfo.Value.IsPremium)
                    {
                        seasonPassNewPopup.Show();
                    }
                }

                if (Widget.TryFind<EventReleaseNotePopup>(out var eventReleaseNotePopup))
                {
                    if (eventReleaseNotePopup.HasUnread)
                    {
                        eventReleaseNotePopup.Show();
                    }
                }
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }
        }
    }
}
