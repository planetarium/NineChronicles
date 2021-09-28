using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class TwoButtonPopup : SystemInfoWidget
    {
        public SubmitButton confirmButton;
        public SubmitButton cancalButton;
        public TextMeshProUGUI contentText;

        private System.Action _confirmCallback;
        private System.Action _cancelCallback;

        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = Confirm;
            CloseWidget = Cancel;
            confirmButton.OnSubmitClick.Subscribe(_ =>
            {
                Confirm();
            }).AddTo(gameObject);
            cancalButton.OnSubmitClick.Subscribe(_ =>
            {
                Cancel();
            }).AddTo(gameObject);
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);
        }

        public void Show(string content, string confirm, string cancel,
            System.Action confirmCallback, System.Action cancelCallback = null)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show( content, confirm, cancel, confirmCallback, cancelCallback);
                return;
            }

            var fixedcontent = content.Replace("\\n", "\n");
            contentText.text = fixedcontent;

            _confirmCallback = confirmCallback;
            _cancelCallback = cancelCallback;

            confirmButton.SetSubmitText(confirm);
            confirmButton.SetSubmittableWithoutInteractable(true);

            cancalButton.SetSubmitText(cancel);
            cancalButton.SetSubmittableWithoutInteractable(true);
            Show();
        }

        private void Confirm()
        {
            _confirmCallback?.Invoke();
            base.Close();
            AudioController.PlayClick();
        }

        public void Cancel()
        {
            _cancelCallback?.Invoke();
            base.Close();
            AudioController.PlayClick();
        }
    }
}
