using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class CostTwoButtonPopup : PopupWidget
    {
        [SerializeField]
        private ConditionalCostButton costButton;

        [SerializeField]
        private TextButton cancelButton;

        [SerializeField]
        private TextMeshProUGUI contentText;

        private readonly List<IDisposable> _disposables = new();
        private System.Action _cancelCallback;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = Cancel;
            cancelButton.OnClick = Cancel;
        }

        public void Show(
            string content, string confirmText, string cancelText, CostType costType, int cost,
            Action<ConditionalButton.State> confirmCallback, System.Action cancelCallback = null)
        {
            if (gameObject.activeSelf)
            {
                Close(true);
                Show(content, confirmText, cancelText, costType, cost, confirmCallback, cancelCallback);
                return;
            }

            _disposables.DisposeAllAndClear();
            costButton.OnClickSubject.Subscribe(state =>
            {
                confirmCallback(state);
                Close();
                AudioController.PlayClick();
            }).AddTo(_disposables);

            _cancelCallback = cancelCallback;

            var fixedContent = content.Replace("\\n", "\n");
            contentText.text = fixedContent;
            cancelButton.Text = cancelText;
            costButton.Text = confirmText;
            costButton.SetCost(costType, cost);
            if (costType == CostType.ActionPoint)
            {
                var apCondition = ConditionalCostButton.CheckCostOfType(costType, cost);
                var apPotionCondition = ConditionalCostButton.CheckCostOfType(CostType.ApPotion, 1);
                costButton.Interactable = apCondition || apPotionCondition;
            }

            base.Show();
        }

        private void Cancel()
        {
            _cancelCallback?.Invoke();
            Close();
            AudioController.PlayClick();
        }
    }
}
