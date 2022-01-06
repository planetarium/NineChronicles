using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class IntroScreen : LoadingScreen
    {
        [SerializeField]
        private Image _background;

        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
            _background.sprite = EventManager.GetIntroSprite();
        }

        public void Show(string keyStorePath, string privateKey)
        {
            indicator.Show("Verifying transaction..");
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            StartLoading();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            indicator.Close();
        }

        private void StartLoading()
        {
            var w = Find<LoginSystem>();
            w.Show(_keyStorePath, _privateKey);
        }
    }
}
