using Nekoyume.EnumType;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI
{
    public class PopupWidget : Widget
    {
        public override WidgetType WidgetType => WidgetType.Popup;
        
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
