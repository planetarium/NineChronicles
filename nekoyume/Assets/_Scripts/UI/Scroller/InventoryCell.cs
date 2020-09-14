using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class InventoryCell : GridCell<
        Model.InventoryItem,
        InventoryScroll.ContextModel>
    {
        [SerializeField]
        private InventoryItemView view = null;

        public InventoryItemView View => view;

        private void Awake()
        {
            view.OnClick
                .Subscribe(item => Context.OnClick.OnNext(this))
                .AddTo(gameObject);

            view.OnDoubleClick
                .Subscribe(item => Context.OnDoubleClick.OnNext(this))
                .AddTo(gameObject);
        }

        public override void UpdateContent(Model.InventoryItem itemData)
        {
            view.SetData(itemData);
        }
    }
}
