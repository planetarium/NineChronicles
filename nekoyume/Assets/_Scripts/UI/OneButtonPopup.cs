using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class OneButtonPopup : SystemInfoWidget
    {
        public SubmitButton confirmButton;
        public TextMeshProUGUI contentText;

        private System.Action _confirmCallback;

        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = Confirm;
            CloseWidget = Confirm;
            confirmButton.OnSubmitClick.Subscribe(_ => Confirm()).AddTo(gameObject);
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);
        }

        public void Show(string content, string confirm, System.Action confirmCallback)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(content, confirm, confirmCallback);
                return;
            }

            var fixedcontent = content.Replace("\\n", "\n");
            contentText.text = fixedcontent;

            _confirmCallback = confirmCallback;

            confirmButton.SetSubmitText(confirm);
            confirmButton.SetSubmittableWithoutInteractable(true);

            Show();
        }

        private void Confirm()
        {
            _confirmCallback?.Invoke();
            base.Close();
            AudioController.PlayClick();
        }
    }
}
