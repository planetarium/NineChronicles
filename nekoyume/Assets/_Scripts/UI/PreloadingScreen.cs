using Nekoyume.UI.Module;

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
            Find<Synopsis>().Show();
            base.Close(ignoreCloseAnimation);
            indicator.Close();
        }
    }
}
