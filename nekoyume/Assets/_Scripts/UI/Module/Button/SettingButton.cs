using Nekoyume.EnumType;

namespace Nekoyume.UI.Module
{
    public class SettingButton : Widget
    {
        public override WidgetType WidgetType => WidgetType.Development;

        protected override void Awake()
        {
        }

        public void ShowSettings()
        {
            Find<Settings>().Show();
        }
    }
}
