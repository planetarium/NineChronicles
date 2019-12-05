using Nekoyume.Game.Controller;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Title : ScreenWidget
    {
        public bool ready = false;
        public Animator animator;

        private string _keyStorePath;
        private string _privateKey;

        public void Show(string keyStorePath, string privateKey)
        {
            base.Show();
            animator.enabled = false;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
        }

        public void OnClick()
        {
            if (!ready)
                return;

            Find<LoginPopup>().Show(_keyStorePath, _privateKey);
        }

        public void Ready()
        {
            ready = true;
            animator.enabled = true;
        }
    }
}
