using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class InventoryViewCell : GridCell<Model.InventoryItemViewModel, InventoryViewScroll.ContextModel>
    {
        [SerializeField]
        private NewInventoryItemView view;

        public override void UpdateContent(Model.InventoryItemViewModel viewModel)
        {
            view.Set(viewModel, Context);

            if (Index == 0)
            {
                Context.FirstItem = viewModel;
            }
        }
    }
}
