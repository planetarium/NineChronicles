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
        public SubmitButton submitButton;
        public TextMeshProUGUI labelNo;
        public GameObject titleBorder;
        public ConfirmDelegate CloseCallback { get; set; }
        public Blur blur;

        private float blurSize;

        protected override void Awake()
        {
            base.Awake();

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
            bool localize = true, float blurSize = 1, bool submittable = true)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(title, content, labelYes, labelNo, localize, blurSize);
                return;
            }

            Set(title, content, labelYes, labelNo, localize, blurSize, submittable);
            Show();
        }

        public void Set(string title, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true, float blurSize = 1, bool submittable = true)
        {
            bool titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                    this.title.text = L10nManager.Localize(title);
                this.content.text = L10nManager.Localize(content);
                submitButton.SetSubmitText(L10nManager.Localize(labelYes));
                this.labelNo.text = L10nManager.Localize(labelNo);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                submitButton.SetSubmitText(labelYes);
                this.labelNo.text = labelNo;
            }

            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);
            this.blurSize = blurSize;
            submitButton.SetSubmittableWithoutInteractable(submittable);
        }

        public void Yes()
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close();
            AudioController.PlayClick();
            CloseCallback?.Invoke(ConfirmResult.Yes);
        }

        public void No()
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close();
            AudioController.PlayClick();
            CloseCallback?.Invoke(ConfirmResult.No);
        }

        public void NoWithoutCallback()
        {
            if (blur)
            {
                blur.Close();
            }

            base.Close();
            AudioController.PlayClick();
        }
    }
}
