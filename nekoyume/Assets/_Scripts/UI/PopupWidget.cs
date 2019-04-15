using Nekoyume.Game.Controller;
using Nekoyume.UI;

namespace _Scripts.UI
{
    public class PopupWidget : Widget
    {
        public override void Show()
        {
            base.Show();
            AudioController.PlayPopup();
        }

        public override void Close()
        {
            Destroy(gameObject);
        }
    }
}