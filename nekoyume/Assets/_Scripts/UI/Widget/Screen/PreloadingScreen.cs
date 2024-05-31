using Nekoyume.Game.Factory;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    public class PreloadingScreen : ScreenWidget
    {
        [SerializeField]
        private LoadingIndicator indicator;

        [SerializeField]
        private VideoPlayer videoPlayer;

        [SerializeField]
        private VideoClip showClip;

        [SerializeField]
        private VideoClip loopClip;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
            videoPlayer.clip = showClip;
            videoPlayer.Prepare();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            var message = L10nManager.Localize("BLOCK_CHAIN_MINING_TX") + "...";
            indicator.Show(message);
            videoPlayer.Play();
            videoPlayer.loopPointReached += OnShowVideoEnded;
        }

        public override async void Close(bool ignoreCloseAnimation = false)
        {
            videoPlayer.Stop();
            if (!GameConfig.IsEditor)
            {
                if (States.Instance.AgentState.avatarAddresses.Any() &&
                    States.Instance.AvatarStates.Any(x => x.Value.level > 49) &&
                    Util.TryGetStoredAvatarSlotIndex(out var slotIndex) &&
                    States.Instance.AvatarStates.ContainsKey(slotIndex))
                {
                    var loadingScreen = Find<LoadingScreen>();
                    loadingScreen.Show(
                        LoadingScreen.LoadingType.Entering,
                        L10nManager.Localize("UI_LOADING_BOOTSTRAP_START"));
                    await RxProps.SelectAvatarAsync(
                        slotIndex, Game.Game.instance.Agent.BlockTipStateRootHash);
                    loadingScreen.Close();
                    Game.Event.OnRoomEnter.Invoke(false);
                    Game.Event.OnUpdateAddresses.Invoke();
                }
                else
                {
                    Find<Synopsis>().Show();
                }
            }
            else
            {
                PlayerFactory.Create();

                if (Util.TryGetStoredAvatarSlotIndex(out var slotIndex) &&
                    States.Instance.AvatarStates.ContainsKey(slotIndex))
                {
                    var avatarState = States.Instance.AvatarStates[slotIndex];
                    if (avatarState?.inventory == null ||
                        avatarState.questList == null ||
                        avatarState.worldInformation == null)
                    {
                        EnterLogin();
                    }
                    else
                    {
                        await RxProps.SelectAvatarAsync(
                            slotIndex, Game.Game.instance.Agent.BlockTipStateRootHash);
                        Game.Event.OnRoomEnter.Invoke(false);
                        Game.Event.OnUpdateAddresses.Invoke();
                    }
                }
                else
                {
                    EnterLogin();
                }
            }

            base.Close(ignoreCloseAnimation);
            indicator.Close();
        }

        private static void EnterLogin()
        {
            Find<Login>().Show();
            Game.Event.OnNestEnter.Invoke();
        }

        private void OnShowVideoEnded(VideoPlayer player)
        {
            player.loopPointReached -= OnShowVideoEnded;
            videoPlayer.clip = loopClip;
            player.isLooping = true;
            videoPlayer.Play();
        }
    }
}
