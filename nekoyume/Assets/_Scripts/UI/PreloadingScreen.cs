using Nekoyume.Game.Factory;
using System.Collections.Generic;
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

        public override void Close(bool ignoreCloseAnimation = false)
        {
            videoPlayer.Stop();
            if (!GameConfig.IsEditor)
            {
                Find<Synopsis>().Show();
            }
            else
            {
                PlayerFactory.Create();

                if (PlayerPrefs.HasKey(LoginDetail.RecentlyLoggedInAvatarKey))
                {
                    var index = PlayerPrefs.GetInt(LoginDetail.RecentlyLoggedInAvatarKey);

                    try
                    {
                        State.States.Instance.SelectAvatar(index);
                        Game.Event.OnRoomEnter.Invoke(false);
                    }
                    catch (KeyNotFoundException e)
                    {
                        Debug.LogWarning(e.Message);
                        EnterLogin();
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

        private void EnterLogin()
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
