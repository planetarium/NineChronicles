using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public enum ConfirmResult : int
    {
        Yes,
        No,
    }

    public delegate void ConfirmDelegate(ConfirmResult result);

    public class Confirm : PopupWidget
    {
        public Text title;
        public Text content;
        public Text labelYes;
        public Text labelNo;
        public ConfirmDelegate CloseCallback { get; set; }

        public void Show(string title, string content, string btnYes = "UI_OK", string btnNo = "UI_CANCEL",
            bool localize = false)
        {
            if (localize)
            {
                this.title.text = LocalizationManager.Localize(title);
                this.content.text = LocalizationManager.Localize(content);
                labelYes.text = LocalizationManager.Localize(btnYes);
                labelNo.text = LocalizationManager.Localize(btnNo);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                labelYes.text = btnYes;
                labelNo.text = btnNo;
            }

            this.title.gameObject.SetActive(!string.IsNullOrEmpty(title));

            base.Show();
        }

        public void Yes()
        {
            if (CloseCallback != null)
            {
                CloseCallback(ConfirmResult.Yes);
            }

            base.Close();
            AudioController.PlayClick();
        }

        public void No()
        {
            if (CloseCallback != null)
            {
                CloseCallback(ConfirmResult.No);
            }

            base.Close();
            AudioController.PlayClick();
        }
    }
}
