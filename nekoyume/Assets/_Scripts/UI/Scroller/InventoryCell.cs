using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class InventoryCell : GridCell<Model.InventoryItem, InventoryScroll.ContextModel>
    {
        [SerializeField]
        private InventoryItemView view;

        public override void UpdateContent(Model.InventoryItem viewModel)
        {
            view.Set(viewModel, Context);

            if (Index == 0)
            {
                Context.FirstItem = viewModel;
            }
        }
    }
}
