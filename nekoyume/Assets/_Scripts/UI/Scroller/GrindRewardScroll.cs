using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class GrindRewardScroll : GridScroll<
        GrindRewardCell.Model,
        GrindRewardScroll.ContextModel,
        GrindRewardScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
        }

        public class CellCellGroup : GridCellGroup<GrindRewardCell.Model, ContextModel>
        {
        }

        [SerializeField] private GrindRewardCell cellTemplate;

        protected override FancyCell<GrindRewardCell.Model, ContextModel> CellTemplate => cellTemplate;
    }
}
