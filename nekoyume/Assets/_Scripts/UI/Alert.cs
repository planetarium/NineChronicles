using UnityEngine.UI;
using _Scripts.UI;


namespace Nekoyume.UI
{
    public delegate void AlertDelegate();
    public class Alert : PopupWidget
    {
        public Text content;
        public Text labelOK;
        public AlertDelegate CloseCallback { get; set; }
        public void Show(string text, string btnOK="OK", bool localize=false)
        {
            if (localize)
            {
                content.text = Assets.SimpleLocalization.LocalizationManager.Localize(text);
                labelOK.text = Assets.SimpleLocalization.LocalizationManager.Localize(btnOK);
            }
            else
            {
                content.text = text;
                labelOK.text = btnOK;
            }
            base.Show();
        }

        override public void Close()
        {
            if (CloseCallback != null)
            {
                CloseCallback();
            }
            base.Close();
        }
    }
}
