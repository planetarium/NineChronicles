using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
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

        private Model.Inventory _data;

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
            _data = new Model.Inventory(AvatarManager.AvatarState.items);
            inventory.SetData(_data);

            base.Show();
        }

        public override void Close()
        {
            _data?.Dispose();
            _data = null;
            
            base.Close();
        }
    }
}
