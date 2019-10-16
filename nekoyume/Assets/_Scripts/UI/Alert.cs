using Assets.SimpleLocalization;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public delegate void AlertDelegate();
    public class Alert : PopupWidget
    {
        public Text title;
        public Text content;
        public Text labelOK;
        public GameObject titleBorder;
        public AlertDelegate CloseCallback { get; set; }
        public void Show(string title, string content, string btnOK = "OK", bool localize = false)
        {
            bool titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                    this.title.text = LocalizationManager.Localize(title);
                this.content.text = LocalizationManager.Localize(content);
                labelOK.text = LocalizationManager.Localize(btnOK);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                labelOK.text = btnOK;
            }

            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);

            base.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            CloseCallback?.Invoke();
            Game.Controller.AudioController.PlayClick();
            base.Close(ignoreCloseAnimation);
        }
    }
}
