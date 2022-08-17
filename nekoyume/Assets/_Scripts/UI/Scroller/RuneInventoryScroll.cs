using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RuneInventoryScroll : RectScroll<RuneInventoryItem, RuneInventoryScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
        }

        [SerializeField]
        private RuneInventoryCell cellTemplate = null;
    }
}
