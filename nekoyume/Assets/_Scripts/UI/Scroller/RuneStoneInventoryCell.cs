using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RuneStoneInventoryCell : RectCell<RuneStoneInventoryItem, RuneStoneInventoryScroll.ContextModel>
    {
        [SerializeField]
        private RuneStoneInventoryItemView view;

        public override void UpdateContent(RuneStoneInventoryItem viewModel)
        {
            view.Set(viewModel, Context);
        }
    }
}
