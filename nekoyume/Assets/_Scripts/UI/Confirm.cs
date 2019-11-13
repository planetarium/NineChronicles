using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
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
        public TextMeshProUGUI title;
        public TextMeshProUGUI content;
        public TextMeshProUGUI labelYes;
        public TextMeshProUGUI labelNo;
        public GameObject titleBorder;
        public ConfirmDelegate CloseCallback { get; set; }

        public void Show(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
        {
            bool titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                    this.title.text = LocalizationManager.Localize(title);
                this.content.text = LocalizationManager.Localize(content);
                this.labelYes.text = LocalizationManager.Localize(labelYes);
                this.labelNo.text = LocalizationManager.Localize(labelNo);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                this.labelYes.text = "OK";
                this.labelNo.text = "CANCEL";
            }

            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);

            base.Show();
        }

        public void Yes()
        {
            CloseCallback?.Invoke(ConfirmResult.Yes);

            base.Close();
            AudioController.PlayClick();
        }

        public void No()
        {
            CloseCallback?.Invoke(ConfirmResult.No);

            base.Close();
            AudioController.PlayClick();
        }
    }
}
