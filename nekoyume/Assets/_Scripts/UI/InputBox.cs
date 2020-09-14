using Nekoyume.Game.Controller;
using Nekoyume.L10n;
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

    public class InputBox : PopupWidget
    {
        public InputField inputField;
        public Text inputFieldPlaceHolder;
        public TextMeshProUGUI content;
        public TextMeshProUGUI labelYes;
        public TextMeshProUGUI labelNo;
        public InputBoxDelegate CloseCallback { get; set; }
        public Blur blur;

        public string text;

        protected override void Awake()
        {
            base.Awake();

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
                this.labelYes.text = L10nManager.Localize(labelYes);
                this.labelNo.text = L10nManager.Localize(labelNo);
            }
            else
            {
                inputFieldPlaceHolder.text = placeHolderText;
                this.content.text = content;
                this.labelYes.text = "OK";
                this.labelNo.text = "CANCEL";
            }

            base.Show();
            blur?.Show();
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

        public override void Close(bool ignoreCloseAnimation = false)
        {
            blur?.Close();
            base.Close(ignoreCloseAnimation);
        }
    }
}
