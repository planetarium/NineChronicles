using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class EquipmentInventoryViewCell : GridCell<Model.EquipmentInventoryViewModel,
        EquipmentInventoryViewScroll.ContextModel>
    {
        [SerializeField]
        private EquipmentInventoryItemView view;

        public override void UpdateContent(Model.EquipmentInventoryViewModel viewModel)
        {
            view.Set(viewModel, Context);
        }
    }
}
