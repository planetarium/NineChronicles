using UnityEditor;

namespace Nekoyume.UI
{
    public class WidgetHandler
    {
        private static WidgetHandler _instance;
        public static WidgetHandler Instance => _instance ?? (_instance = new WidgetHandler());

        public bool isActiveTutorialMaskWidget { get; set; }
        public MessageCatManager messageCatManager { private get; set; }

        public void HideAllMessageCat()
        {
            if (messageCatManager is null)
            {
                throw new WidgetNotFoundException("MessageCatManager");
            }

            messageCatManager.HideAll(false);
        }
    }
}
