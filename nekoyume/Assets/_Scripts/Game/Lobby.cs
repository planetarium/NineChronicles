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

        private void Awake()
        {
            Event.OnLobbyEnter.AddListener(OnLobbyEnter);
        }

        private void OnDestroy()
        {
            Event.OnLobbyEnter.RemoveListener(OnLobbyEnter);
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
                var seasonPassNewPopup = Widget.Find<SeasonPassNewPopup>();
                if (seasonPassNewPopup.HasUnread && avatarInfo.HasValue &&
                    !avatarInfo.Value.IsPremium)
                {
                    seasonPassNewPopup.Show();
                }

                var eventReleaseNotePopup = Widget.Find<EventReleaseNotePopup>();
                if (eventReleaseNotePopup.HasUnread)
                {
                    eventReleaseNotePopup.Show();
                }
            }
            catch (Exception e)
            {
                NcDebug.LogError(e);
            }
        }
    }
}
