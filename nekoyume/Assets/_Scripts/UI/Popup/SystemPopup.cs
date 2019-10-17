using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public abstract class SystemPopup : MessageBox
    {
        public override WidgetType WidgetType => WidgetType.SystemInfo;
    }
}
