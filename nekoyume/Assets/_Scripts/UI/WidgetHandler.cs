using Nekoyume.UI.Module;
using UnityEditor;

namespace Nekoyume.UI
{
    public class WidgetHandler
    {
        private static WidgetHandler _instance;
        public static WidgetHandler Instance => _instance ?? (_instance = new WidgetHandler());
        private MessageCatManager _messageCatManager;
        private HeaderMenu _headerMenu;
        private Battle _battle;
        private Menu _menu;

        public bool IsActiveTutorialMaskWidget { get; set; }

        public MessageCatManager MessageCatManager =>
            _messageCatManager
                ? _messageCatManager
                : (_messageCatManager = Widget.Find<MessageCatManager>());

        public Battle Battle => _battle ? _battle : (_battle = Widget.Find<Battle>());
        public Menu Menu => _menu ? _menu : (_menu = Widget.Find<Menu>());

        public void HideAllMessageCat()
        {
            try
            {
                MessageCatManager.HideAll(false);
            }
            catch (WidgetNotFoundException)
            {
                // Do Nothing.
            }
        }
    }
}
