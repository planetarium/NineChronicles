namespace Nekoyume.UI
{
    public class IntroScreen : LoadingScreen
    {
        private string _keyStorePath;
        private string _privateKey;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
        }

        public void Show(string keyStorePath, string privateKey)
        {
            indicator.Show("Verifying transaction..");
            _keyStorePath = keyStorePath;
            _privateKey = privateKey;
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
