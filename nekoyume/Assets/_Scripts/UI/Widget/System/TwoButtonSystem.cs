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
        private ConditionalCostButton costButton = null;

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
            if (costButton)
            {
                costButton.OnClickSubject.Subscribe(_ => Confirm()).AddTo(gameObject);
            }
        }

        public void Show(string content, string confirmText, string cancelText,
            System.Action confirmCallback, System.Action cancelCallback = null,
            CostType costType = CostType.None, int cost = 0)
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

            cancelButton.Text = cancelText;
            if (costType == CostType.None || cost == 0)
            {
                if (costButton)
                {
                    costButton.gameObject.SetActive(false);
                }

                confirmButton.gameObject.SetActive(true);
                confirmButton.Text = confirmText;
            }
            else
            {
                confirmButton.gameObject.SetActive(false);
                costButton.gameObject.SetActive(true);
                costButton.Text = confirmText;
                costButton.SetCost(costType, cost);
            }

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
