
using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class ScreenWidget : Widget
    {
        public override WidgetType WidgetType => WidgetType.Screen;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
        }
    }
}
