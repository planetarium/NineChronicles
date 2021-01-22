using Nekoyume.Game.Controller;
using UnityEngine;
using mixpanel;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class Title : ScreenWidget
    {
        [SerializeField]
        private SettingButton settingButton = null;

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            Mixpanel.Track("Unity/TitleImpression");
            AudioController.instance.PlayMusic(AudioController.MusicCode.Title);
            settingButton.Show();
        }
    }
}
