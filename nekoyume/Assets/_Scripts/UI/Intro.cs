using mixpanel;

namespace Nekoyume.UI
{
    public class Intro : Widget
    {
        private string _keyStorePath;
        private string _privateKey;

        public void Show(string keyStorePath, string privateKey)
        {
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
            StartLoading();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            Find<Title>().Show();
        }

        private void StartLoading()
        {
            var w = Find<LoginPopup>();
            w.Show(_keyStorePath, _privateKey);
            Find<PreloadingScreen>().Show();
            Mixpanel.Track("Unity/Click Main Logo");
        }
    }
}
