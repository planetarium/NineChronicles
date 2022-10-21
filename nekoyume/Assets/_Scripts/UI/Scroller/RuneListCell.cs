using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RuneListCell : GridCell<Model.RuneListItem, RuneListScroll.ContextModel>
    {
        [SerializeField]
        private RuneListView view;

        public override void UpdateContent(Model.RuneListItem itemData)
        {
            view.Set(itemData, Context);
            if (Index == 0)
            {
                Context.FirstItem = itemData;
            }
        }
    }
}
