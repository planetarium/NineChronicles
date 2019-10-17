using Assets.SimpleLocalization;
using System;
using UnityEngine.UI;

[Flags]
public enum MessageBoxType
{
    OK,
    Cancel,

}

namespace Nekoyume.UI
{
    public delegate void MessageBoxDelegate();
    public class MessageBox : PopupWidget
    {
        public Text title;
        public Text content;
        public Text labelSubmit;
        public Button btnCancel;
        public MessageBoxDelegate SubmitCallback { get; set; }
        public void Show(string title, string content, MessageBoxType messageBoxType, string btnSubmit = "OK", bool localize = false)
        {
            bool hasCancel = messageBoxType.HasFlag(MessageBoxType.Cancel);

            if (localize)
            {
                this.title.text = LocalizationManager.Localize(title);
                this.content.text = LocalizationManager.Localize(content);
                labelSubmit.text = LocalizationManager.Localize(btnSubmit);
                if (hasCancel)
                    btnCancel.GetComponentInChildren<Text>().text = LocalizationManager.Localize("UI_CANCEL");
            }
            else
            {
                this.title.text = title;
                this.content.text = content;
                labelSubmit.text = btnSubmit;
            }

            if (hasCancel)
            {
                btnCancel.gameObject.SetActive(true);
            }

            this.title.gameObject.SetActive(!string.IsNullOrEmpty(title));

            base.Show();
        }

        public void Submit()
        {
            SubmitCallback?.Invoke();
            Close();
        }

        public override void Close()
        {
            Game.Controller.AudioController.PlayClick();
            base.Close();
        }
    }
}
