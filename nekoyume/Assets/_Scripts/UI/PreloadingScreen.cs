using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class PreloadingScreen : LoadingScreen
    {
        public LoadingIndicator indicator;

        protected override void Awake()
        {
            base.Awake();
            indicator.Close();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            indicator.Show("UI_PRELOADING_MESSAGE");
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<Synopsis>().Show();
            base.Close(ignoreCloseAnimation);
            indicator.Close();
        }
    }
}
