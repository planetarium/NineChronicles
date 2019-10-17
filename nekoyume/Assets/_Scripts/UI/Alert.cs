using Assets.SimpleLocalization;
using UnityEngine.UI;
using UnityEngine;

namespace Nekoyume.UI
{
    public delegate void CloseDelegate();
    public class Alert : PopupWidget
    {
        public Text title;
        public Text content;
        public Text labelSubmit;
        public GameObject titleBorder;
        public CloseDelegate SubmitCallback { get; set; }
        public void Show(string title, string content, string btnSubmit = "OK", bool localize = false)
        {
            if (localize)
            {
                this.title.text = LocalizationManager.Localize(title);
                this.content.text = LocalizationManager.Localize(content);
                labelSubmit.text = LocalizationManager.Localize(btnSubmit);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                labelSubmit.text = btnSubmit;
            }

            bool titleExists = !string.IsNullOrEmpty(title);
            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);

            base.Show();
        }

        public override void Close()
        {
            SubmitCallback?.Invoke();
            Game.Controller.AudioController.PlayClick();
            base.Close();
        }
    }
}
