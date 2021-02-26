using Nekoyume.L10n;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public delegate void AlertDelegate();
    public class Alert : PopupWidget
    {
        public TextMeshProUGUI title;
        public TextMeshProUGUI content;
        public TextMeshProUGUI labelOK;
        public GameObject titleBorder;
        public AlertDelegate CloseCallback { get; set; }
        public Blur blur;

        private float blurRadius;

        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = () => Close();
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);

            if (blur)
            {
                blur.Show(radius: blurRadius);
            }
        }

        public virtual void Show(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(title, content, labelOK, localize);
                return;
            }

            Set(title, content, labelOK, localize);
            Show();
        }

        public void Set(string title, string content, string labelOK = "UI_OK", bool localize = true, float blurRadius = 1)
        {
            bool titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                    this.title.text = L10nManager.Localize(title);
                this.content.text = L10nManager.Localize(content);
                this.labelOK.text = L10nManager.Localize(labelOK);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                this.labelOK.text = labelOK;
            }

            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);
            this.blurRadius = blurRadius;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            blur?.Close();
            CloseCallback?.Invoke();
            Game.Controller.AudioController.PlayClick();
            base.Close(ignoreCloseAnimation);
        }
    }
}
