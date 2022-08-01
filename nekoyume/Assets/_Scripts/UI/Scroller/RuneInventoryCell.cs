using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RuneInventoryCell : RectCell<RuneInventoryItem, RuneInventoryScroll.ContextModel>
    {
        [SerializeField]
        private RuneInventoryItemView view;

        public override void UpdateContent(RuneInventoryItem viewModel)
        {
            view.Set(viewModel, Context);
        }
    }
}
