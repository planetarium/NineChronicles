using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class MailInfo : MonoBehaviour
    {
        public Image icon;
        public Text label;
        public Game.Mail.Mail data;
        public Button button;

        public void Set(Game.Mail.Mail mail)
        {
            data = mail;
            var sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_quest_01");
            var text = mail.ToInfo();
            var color = ColorHelper.HexToColorRGB("fff9dd");
            if (mail.New)
            {
                button.interactable = true;
            }
            else
            {
                sprite = Resources.Load<Sprite>("UI/Textures/UI_icon_quest_02");
                color = ColorHelper.HexToColorRGB("7a7a7a");
                button.interactable = false;
            }
            icon.sprite = sprite;
            icon.SetNativeSize();
            label.text = text;
            label.color = color;
        }

        public void Read()
        {
            data.New = false;
            button.interactable = false;
            label.color = ColorHelper.HexToColorRGB("7a7a7a");
        }
    }
}
