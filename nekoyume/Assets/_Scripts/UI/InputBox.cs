using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using TMPro;
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
        public InputField inputField;
        public Text inputFieldPlaceHolder;
        public TextMeshProUGUI content;
        public TextMeshProUGUI labelYes;
        public TextMeshProUGUI labelNo;
        public InputBoxDelegate CloseCallback { get; set; }
        public string text;

        public void Show(string placeHolderText, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
        {
            var blur = Find<ModuleBlur>();
            blur.onClick = () => No();
            blur?.Show();

            text = inputField.text = string.Empty;
            if (localize)
            {
                this.inputFieldPlaceHolder.text = LocalizationManager.Localize(placeHolderText);
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

            base.Show();
        }

        public void Yes()
        {
            text = inputField.text;
            CloseCallback?.Invoke(ConfirmResult.Yes);
            Find<ModuleBlur>()?.Close();

            base.Close();
            AudioController.PlayClick();
        }

        public void No()
        {
            text = inputField.text = string.Empty;
            CloseCallback?.Invoke(ConfirmResult.No);
            Find<ModuleBlur>()?.Close();

            base.Close();
            AudioController.PlayClick();
        }
    }
}
