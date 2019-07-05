using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
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

        public Model.Inventory Model { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            
            this.ComponentFieldsNotNullTest();

            closeButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Find<Status>()?.CloseInventory();
            }).AddTo(this);
        }

        public override void Show()
        {
            Model = new Model.Inventory(States.Instance.currentAvatarState.Value.inventory);
            Model.selectedItem.Subscribe(SubscribeSelectedItem);
            
            inventory.SetData(Model);

            base.Show();
        }

        public override void Close()
        {
            Model?.Dispose();
            Model = null;
            
            base.Close();
        }
        
        private void SubscribeSelectedItem(InventoryItem value)
        {
            if (value is null)
            {
                return;
            }
            
            var model = new Model.ItemInformationTooltip(value);
            model.target.Value = GetComponent<RectTransform>();
            Find<ItemInformationTooltip>()?.Show(model);
        }
    }
}
