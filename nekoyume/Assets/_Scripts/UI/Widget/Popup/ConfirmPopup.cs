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
        public Blur blur;

        private float blurSize;

        protected override void Awake()
        {
            base.Awake();

            buttonNo.OnClick = No;
            CloseWidget = NoWithoutCallback;
            SubmitWidget = Yes;
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);

            if (blur)
            {
                blur.Show(size: blurSize);
            }
        }

        public void Show(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, float blurSize = 1)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(title, content, labelYes, labelNo, localize, blurSize);
                return;
            }

            Set(title, content, labelYes, labelNo, localize, blurSize);
            Show();
        }

        public void Set(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, float blurSize = 1)
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
            this.blurSize = blurSize;
        }

        public void Yes()
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close();
            CloseCallback?.Invoke(ConfirmResult.Yes);
        }

        public void No()
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close();
            CloseCallback?.Invoke(ConfirmResult.No);
        }

        public void NoWithoutCallback()
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close();
        }
    }
}
