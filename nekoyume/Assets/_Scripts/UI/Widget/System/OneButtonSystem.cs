using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class OneButtonSystem : SystemWidget
    {
        [SerializeField]
        private TextButton confirmButton = null;

        [SerializeField]
        private TextMeshProUGUI contentText = null;

        private System.Action _confirmCallback;

        protected override void Awake()
        {
            base.Awake();

            SubmitWidget = Confirm;
            CloseWidget = Confirm;
            confirmButton.OnClick = Confirm;
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);
        }

        public void Show(string content, string confirmText, System.Action confirmCallback)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(content, confirmText, confirmCallback);
                return;
            }

            var fixedcontent = content.Replace("\\n", "\n");
            contentText.text = fixedcontent;
            _confirmCallback = confirmCallback;
            confirmButton.Text = confirmText;

            Show();
        }

        private void Confirm()
        {
            NcDebug.Log($"[OneButtonSystem] Confirm() invoked. {confirmButton.Text}");
            _confirmCallback?.Invoke();
            base.Close();
            AudioController.PlayClick();
        }
    }
}
