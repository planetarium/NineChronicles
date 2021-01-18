using Nekoyume.Game.Factory;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    public class PreloadingScreen : LoadingScreen
    {

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            if (!string.IsNullOrEmpty(Message))
            {
                indicator.Show(Message);
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
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
    }
}
