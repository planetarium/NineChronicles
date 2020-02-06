using System;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    /// <summary>
    /// Fix me.
    /// Status 위젯과 함께 사용할 때에는 해당 위젯 하위에 포함되어야 함.
    /// 지금은 별도의 위젯으로 작동하는데, 이 때문에 위젯 라이프 사이클의 일관성을 잃음.(스스로 닫으면 안 되는 예외 발생)
    /// </summary>
    public class Inventory : Widget
    {
        public Module.Inventory inventory;
        public Button closeButton;
        public Blur blur;

        private IDisposable _disposableForSelectItem;

        protected override void Awake()
        {
            base.Awake();

            closeButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<Status>()?.CloseInventory();
            }).AddTo(gameObject);

            CloseWidget = closeButton.onClick.Invoke;
        }

        #region Widget

        public override void Initialize()
        {
            base.Initialize();
            inventory.SharedModel.SelectedItemView.Subscribe(SubscribeSelectedItemView).AddTo(gameObject);
        }

        public override void Show()
        {
            base.Show();
            inventory.SharedModel.State.Value = ItemType.Equipment;
            blur?.Show();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            blur?.Close();
            base.Close(ignoreCloseAnimation);
        }

        #endregion

        private void SubscribeSelectedItemView(InventoryItemView view)
        {
            if (view is null ||
                view.RectTransform == inventory.Tooltip.Target)
            {
                inventory.Tooltip.Close();

                return;
            }

            inventory.Tooltip.Show(
                view.RectTransform,
                view.Model,
                tooltip => inventory.SharedModel.DeselectItemView());
        }
    }
}
