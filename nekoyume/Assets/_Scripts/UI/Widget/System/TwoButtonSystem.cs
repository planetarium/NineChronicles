using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI
{
    public class TwoButtonSystem : SystemWidget
    {
        [SerializeField]
        private TextButton confirmButton = null;

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

        public void Show(string content, string confirmText, string cancelText,
            System.Action confirmCallback, System.Action cancelCallback = null)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(content, confirmText, cancelText, confirmCallback, cancelCallback);
                return;
            }

            var fixedcontent = content.Replace("\\n", "\n");
            contentText.text = fixedcontent;

            _confirmCallback = confirmCallback;
            _cancelCallback = cancelCallback;

            confirmButton.Text = confirmText;
            cancelButton.Text = cancelText;
            base.Show();
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
