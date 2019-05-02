using UnityEngine.UI;

namespace Nekoyume.UI
{
    public delegate void AlertDelegate();
    public class Alert : PopupWidget
    {
        public Text title;
        public Text content;
        public Text labelOK;
        public AlertDelegate CloseCallback { get; set; }
        public void Show(string title, string content, string btnOK="OK", bool localize=false)
        {
            if (localize)
            {
                this.title.text = Assets.SimpleLocalization.LocalizationManager.Localize(title);
                this.content.text = Assets.SimpleLocalization.LocalizationManager.Localize(content);
                labelOK.text = Assets.SimpleLocalization.LocalizationManager.Localize(btnOK);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                labelOK.text = btnOK;
            }

            this.title.gameObject.SetActive(!string.IsNullOrEmpty(title));

            base.Show();
        }

        public override void Close()
        {
            if (CloseCallback != null)
            {
                CloseCallback();
            }
            Game.Controller.AudioController.PlayClick();
            base.Close();
        }
    }
}
