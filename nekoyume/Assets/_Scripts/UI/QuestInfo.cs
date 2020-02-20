using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI
{
    public class QuestInfo : MonoBehaviour
    {
        public Image icon;
        public Text label;
        public QuestModel data;

        public void Set(QuestModel quest)
        {
            data = quest;
            Sprite sprite;
            var text = quest.GetContent();
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
