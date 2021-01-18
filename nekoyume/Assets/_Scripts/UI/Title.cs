using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.EventSystems;
using mixpanel;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class Title : ScreenWidget
    {
        [SerializeField]
        private SettingButton settingButton = null;

        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
        }

        public void ShowLocalizedObjects()
        {
            settingButton.Show();
        }

        public void Show(string keyStorePath, string privateKey)
        {
            base.Show();
            Mixpanel.Track("Unity/TitleImpression");
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            StartLoading();
        }

        public void StartLoading()
        {
            var w = Find<LoginPopup>();
            w.Show(_keyStorePath, _privateKey);
            Find<PreloadingScreen>().Show();
            Mixpanel.Track("Unity/Click Main Logo");
        }
    }
}
