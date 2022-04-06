using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
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

    public class ConfirmPopup : PopupWidget
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI content;
        public TextButton buttonYes;
        public TextButton buttonNo;
        public GameObject titleBorder;
        public ConfirmDelegate CloseCallback { get; set; }

        protected override void Awake()
        {
            base.Awake();

            buttonNo.OnClick = No;
            buttonYes.OnClick = Yes;
            CloseWidget = NoWithoutCallback;
            SubmitWidget = Yes;
        }

        public void Show(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
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
            bool localize = true)
        {
            bool titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                    this.title.text = L10nManager.Localize(title);
                this.content.text = L10nManager.Localize(content);
                buttonYes.Text = L10nManager.Localize(labelYes);
                buttonNo.Text = L10nManager.Localize(labelNo);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                buttonYes.Text = labelYes;
                buttonNo.Text = labelNo;
            }

            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);
        }

        public void Yes()
        {
            base.Close();
            CloseCallback?.Invoke(ConfirmResult.Yes);
        }

        public void No()
        {
            base.Close();
            CloseCallback?.Invoke(ConfirmResult.No);
        }

        public void NoWithoutCallback()
        {
            base.Close();
        }
    }
}
