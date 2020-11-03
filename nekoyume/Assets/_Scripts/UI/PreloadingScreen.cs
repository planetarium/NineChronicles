using Nekoyume.Game.Factory;

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
                Game.Event.OnNestEnter.Invoke();
                Find<Login>().Show();
            }
            base.Close(ignoreCloseAnimation);
            indicator.Close();
        }
    }
}
