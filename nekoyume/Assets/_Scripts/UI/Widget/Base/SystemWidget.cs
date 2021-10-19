using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class SystemWidget : Widget
    {
        public override WidgetType WidgetType => WidgetType.System;
        public override CloseKeyType CloseKeyType => CloseKeyType.Escape;
    }
}
