using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class EquipmentInventoryCell : GridCell<Model.EquipmentInventoryItem,
        EquipmentInventoryScroll.ContextModel>
    {
        [SerializeField]
        private EquipmentInventoryItemView view;

        public override void UpdateContent(Model.EquipmentInventoryItem viewModel)
        {
            view.Set(viewModel, Context);
        }
    }
}
