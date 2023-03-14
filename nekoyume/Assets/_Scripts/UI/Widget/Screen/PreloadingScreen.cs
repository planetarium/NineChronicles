using Libplanet;
using Nekoyume.Game.Factory;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    public class PreloadingScreen : LoadingScreen
    {
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
            if (!string.IsNullOrEmpty(Message))
            {
                indicator.Show(Message);
            }

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
                    var loadingScreen = Find<DataLoadingScreen>();
                    loadingScreen.Message = L10nManager.Localize("UI_LOADING_BOOTSTRAP_START");
                    loadingScreen.Show();
                    await RxProps.SelectAvatarAsync(slotIndex);
                    await WorldBossStates.Set(States.Instance.CurrentAvatarState.address);
                    await States.Instance.InitRuneStoneBalance();
                    await States.Instance.InitSoulStoneBalance();
                    await States.Instance.InitRuneStates();
                    await States.Instance.InitRuneSlotStates();
                    await States.Instance.InitItemSlotStates();
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
                        await RxProps.SelectAvatarAsync(slotIndex);
                        await WorldBossStates.Set(States.Instance.CurrentAvatarState.address);
                        await States.Instance.InitRuneStoneBalance();
                        await States.Instance.InitSoulStoneBalance();
                        await States.Instance.InitRuneStates();
                        await States.Instance.InitRuneSlotStates();
                        await States.Instance.InitItemSlotStates();
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
