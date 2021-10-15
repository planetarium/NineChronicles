using Nekoyume.UI.Module;
using UnityEditor;

namespace Nekoyume.UI
{
    public class WidgetHandler
    {
        private static WidgetHandler _instance;
        public static WidgetHandler Instance => _instance ?? (_instance = new WidgetHandler());
        private MessageCatTooltip _messageCatTooltip;
        private HeaderMenuStatic _headerMenuStatic;
        private Battle _battle;
        private Menu _menu;

        public bool IsActiveTutorialMaskWidget { get; set; }

        public MessageCatTooltip MessageCatTooltip =>
            _messageCatTooltip
                ? _messageCatTooltip
                : (_messageCatTooltip = Widget.Find<MessageCatTooltip>());

        public Battle Battle => _battle ? _battle : (_battle = Widget.Find<Battle>());
        public Menu Menu => _menu ? _menu : (_menu = Widget.Find<Menu>());

        public void HideAllMessageCat()
        {
            try
            {
                MessageCatTooltip.HideAll(false);
            }
            catch (WidgetNotFoundException)
            {
                // Do Nothing.
            }
        }
    }
}
