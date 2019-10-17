using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public abstract class SystemPopup : Alert
    {
        public override WidgetType WidgetType => WidgetType.SystemInfo;
    }
}
