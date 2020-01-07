using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class QuestInfo : MonoBehaviour
    {
        public Image icon;
        public Text label;
        public Game.Quest.Quest data;

        public void Set(Game.Quest.Quest quest)
        {
            data = quest;
            Sprite sprite;
            var text = quest.GetName();
            var color = ColorHelper.HexToColorRGB("fff9dd");
            if (quest.Complete)
            {
                sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_quest_02");
                color = ColorHelper.HexToColorRGB("7a7a7a");
            }
            else
            {
                sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_quest_01");
            }

            icon.sprite = sprite;
            icon.SetNativeSize();
            label.text = text;
            label.color = color;
        }
    }
}
