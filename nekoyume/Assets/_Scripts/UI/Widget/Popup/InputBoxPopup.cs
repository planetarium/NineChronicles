using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public enum InputBoxResult : int
    {
        Yes,
        No,
    }

    public delegate void InputBoxDelegate(ConfirmResult result);

    public class InputBoxPopup : PopupWidget
    {
        public InputField inputField;
        public Text inputFieldPlaceHolder;
        public TextMeshProUGUI content;
        public TextButton submitButton;
        public TextButton cancelButton;

        public InputBoxDelegate CloseCallback { get; set; }

        public string text;

        protected override void Awake()
        {
            base.Awake();

            cancelButton.OnClick = No;
            submitButton.OnClick = Yes;

            CloseWidget = No;
            SubmitWidget = Yes;
        }

        public void Show(string placeHolderText, string content, string labelYes = "UI_OK", string labelNo = "UI_CANCEL",
            bool localize = true)
        {

            text = inputField.text = string.Empty;
            if (localize)
            {
                inputFieldPlaceHolder.text = L10nManager.Localize(placeHolderText);
                this.content.text = L10nManager.Localize(content);
                submitButton.Text = L10nManager.Localize(labelYes);
                cancelButton.Text = L10nManager.Localize(labelNo);
            }
            else
            {
                inputFieldPlaceHolder.text = placeHolderText;
                this.content.text = content;
                submitButton.Text = "OK";
                cancelButton.Text = "CANCEL";
            }

            base.Show();
            inputField.Select();

            Observable.NextFrame().Subscribe(_ =>
            {
                inputField.placeholder.transform.SetAsFirstSibling();
                inputField.textComponent.transform.SetAsFirstSibling();
            });
        }

        public void Yes()
        {
            text = inputField.text;
            CloseCallback?.Invoke(ConfirmResult.Yes);
            Close();
            AudioController.PlayClick();
        }

        public void No()
        {
            text = inputField.text = string.Empty;
            CloseCallback?.Invoke(ConfirmResult.No);
            Close();
            AudioController.PlayClick();
        }
    }
}
