using Nekoyume.EnumType;

namespace Nekoyume.UI.Module
{
    public class SettingButton : Widget
    {
        public override WidgetType WidgetType => WidgetType.Development;

        public void ShowSettings()
        {
            Find<Settings>().Show();
        }
    }
}
