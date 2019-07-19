using Nekoyume.BlockChain;
using Nekoyume.Game.Controller;
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
            Model.selectedItemView.Subscribe(view =>
            {
                if (view is null)
                {
                    return;
                }
                
                if (inventory.Tooltip.Model.target.Value == view.RectTransform)
                {
                    inventory.Tooltip.Close();

                    return;
                }
                
                inventory.Tooltip.Show(view.RectTransform, view.Model);
            });
            
            inventory.SetData(Model);

            base.Show();
        }

        public override void Close()
        {
            Model?.Dispose();
            Model = null;
            
            base.Close();
        }
    }
}
