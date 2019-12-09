using Assets.SimpleLocalization;
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

        public override void Show()
        {
            try
            {
                var blur = Find<ModuleBlur>();
                blur.onClick = () => Close();
                blur.Show();
            }
            catch (WidgetNotFoundException)
            {
            }
            base.Show();
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

        public void Set(string title, string content, string labelOK = "UI_OK", bool localize = true)
        {
            bool titleExists = !string.IsNullOrEmpty(title);
            if (localize)
            {
                if (titleExists)
                    this.title.text = LocalizationManager.Localize(title);
                this.content.text = LocalizationManager.Localize(content);
                this.labelOK.text = LocalizationManager.Localize(labelOK);
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                this.labelOK.text = labelOK;
            }

            this.title.gameObject.SetActive(titleExists);
            titleBorder.SetActive(titleExists);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            CloseCallback?.Invoke();
            Game.Controller.AudioController.PlayClick();
            Find<ModuleBlur>()?.Close(ignoreCloseAnimation);
            base.Close(ignoreCloseAnimation);
        }
    }
}
