using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;

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
        public Blur blur;

        private float blurRadius;

        public override void Show()
        {
            base.Show();
            blur?.Show(radius:blurRadius);
        }

        public void Show(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, float blurRadius = 1)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(title, content, labelYes, labelNo, localize);
                return;
            }

            Set(title, content, labelYes, labelNo, localize);
            Show();
        }

        public void Set(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, float blurRadius = 1)
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
                this.labelYes.text = labelYes;
                this.labelNo.text = labelNo;
            }

            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);
            this.blurRadius = blurRadius;
        }

        public void Yes()
        {
            blur?.Close();

            base.Close();
            AudioController.PlayClick();
            CloseCallback?.Invoke(ConfirmResult.Yes);
        }

        public void No()
        {
            blur?.Close();

            base.Close();
            AudioController.PlayClick();
            CloseCallback?.Invoke(ConfirmResult.No);
        }

        public void NoWithoutCallback()
        {
            blur?.Close();
            
            base.Close();
            AudioController.PlayClick();
        }
    }
}
