using Nekoyume.EnumType;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI
{
    public class PopupWidget : Widget
    {
        protected override WidgetType WidgetType => WidgetType.Popup;

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            PlayPopupSound();
        }

        protected virtual void PlayPopupSound()
        {
            AudioController.PlayPopup();
        }
    }
}
