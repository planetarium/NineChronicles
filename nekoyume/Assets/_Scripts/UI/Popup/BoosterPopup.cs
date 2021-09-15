using Nekoyume.Game;

namespace Nekoyume.UI
{
    public class BoosterPopup : PopupWidget
    {
        private Stage _stage;

        public void Show(Stage stage)
        {
            _stage = stage;
            Show();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }
    }
}
