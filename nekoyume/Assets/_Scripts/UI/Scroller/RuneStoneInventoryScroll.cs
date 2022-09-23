using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class RuneStoneInventoryScroll : RectScroll<RuneStoneInventoryItem, RuneStoneInventoryScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
        }

        [SerializeField]
        private RuneStoneInventoryCell cellTemplate = null;
    }
}
