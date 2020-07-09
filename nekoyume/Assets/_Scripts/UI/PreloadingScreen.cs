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

        protected override void Update()
        {
            base.Update();
            if (!string.IsNullOrEmpty(Message) && loadingText?.text != Message)
            {
                if (indicator.gameObject.activeSelf)
                {
                    indicator.UpdateMessage(Message);
                }
                else
                {
                    indicator.Show(Message);
                }
            }
        }
    }
}
