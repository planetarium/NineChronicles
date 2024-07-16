using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class SummonSkillsScroll : GridScroll<SummonSkillsCell.Model, SummonSkillsScroll.ContextModel, SummonSkillsScroll.CellCellGroup>
    {
        public class ContextModel : GridScrollDefaultContext
        {
        }

        public class CellCellGroup : GridCellGroup<SummonSkillsCell.Model, ContextModel>
        {
        }

        [SerializeField]
        private SummonSkillsCell cellPrefab;

        protected override FancyCell<SummonSkillsCell.Model, ContextModel> CellTemplate => cellPrefab;
    }
}
