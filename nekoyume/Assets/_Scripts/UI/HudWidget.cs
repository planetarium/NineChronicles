using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class HudWidget : Widget
    {
        protected override WidgetType WidgetType => WidgetType.Hud;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Destroy(gameObject);
        }
    }
}
