using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class PetSlotCell : GridCell<PetSlotViewModel, PetSlotScroll.ContextModel>
    {
        [SerializeField]
        private PetSlotView view;

        public override void UpdateContent(PetSlotViewModel itemData)
        {
            view.Set(itemData, Context);
            if (Index == 0)
            {
                Context.FirstItem = itemData;
            }
        }
    }
}
