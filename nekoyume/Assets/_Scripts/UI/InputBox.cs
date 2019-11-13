using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public enum InputBoxResult : int
    {
        Yes,
        No,
    }

    public delegate void InputBoxDelegate(ConfirmResult result);

    public class InputBox : PopupWidget
    {
        public TMP_InputField inputField;
        public TextMeshProUGUI inputFieldPlaceHolder;
        public TextMeshProUGUI content;
        public Text labelYes;
        public Text labelNo;
        public InputBoxDelegate CloseCallback { get; set; }
        public string text;

        public void Show(string placeHolderText, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
        {
            if (localize)
            {
                this.inputFieldPlaceHolder.text = LocalizationManager.Localize(content);
                this.content.text = LocalizationManager.Localize(content);
                this.labelYes.text = LocalizationManager.Localize(labelYes);
                this.labelNo.text = LocalizationManager.Localize(labelNo);
            }
            else
            {
                this.inputFieldPlaceHolder.text = placeHolderText;
                this.content.text = content;
                this.labelYes.text = "OK";
                this.labelNo.text = "CANCEL";
            }

            text = inputField.text = string.Empty;
            base.Show();
        }

        public void Yes()
        {
            CloseCallback?.Invoke(ConfirmResult.Yes);
            text = inputField.text;

            base.Close();
            AudioController.PlayClick();
        }

        public void No()
        {
            CloseCallback?.Invoke(ConfirmResult.No);
            text = inputField.text = string.Empty;

            base.Close();
            AudioController.PlayClick();
        }
    }
}
