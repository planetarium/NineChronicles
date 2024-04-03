using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class TwoButtonSystem : SystemWidget
    {
        [SerializeField]
        protected TextButton confirmButton = null;

        [SerializeField]
        private TextButton cancelButton = null;

        [SerializeField]
        private TextMeshProUGUI contentText = null;

        private System.Action _confirmCallback;
        private System.Action _cancelCallback;

        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = Confirm;
            CloseWidget = Cancel;
            confirmButton.OnClick = Confirm;
            cancelButton.OnClick = Cancel;
        }

        public void Show(
            string content,
            string confirmText,
            string cancelText,
            System.Action confirmCallback,
            System.Action cancelCallback = null)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(content, confirmText, cancelText, confirmCallback, cancelCallback);
                return;
            }

            var fixedContent = content.Replace("\\n", "\n");
            contentText.text = fixedContent;

            _confirmCallback = confirmCallback;
            _cancelCallback = cancelCallback;

            confirmButton.Text = confirmText;
            cancelButton.Text = cancelText;

            base.Show();
        }

        private void Confirm()
        {
            NcDebug.Log($"[TwoButtonSystem] Confirm() invoked. {confirmButton.Text}");
            _confirmCallback?.Invoke();
            base.Close();
            AudioController.PlayClick();
        }

        public void Cancel()
        {
            NcDebug.Log($"[TwoButtonSystem] Cancel() invoked. {cancelButton.Text}");
            _cancelCallback?.Invoke();
            base.Close();
            AudioController.PlayClick();
        }
    }
}
