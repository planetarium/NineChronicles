using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.State;
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
                var inventoryItems = States.Instance.CurrentAvatarState.inventory.Items;
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                var apStoneCount = inventoryItems.Where(x =>
                        x.item.ItemSubType == ItemSubType.ApStone &&
                        !x.Locked &&
                        !(x.item is ITradableItem tradableItem &&
                          tradableItem.RequiredBlockIndex > blockIndex))
                    .Sum(item => item.count);
                costButton.Interactable = apStoneCount > 0;
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
