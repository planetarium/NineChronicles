using Nekoyume.TableData;
using Nekoyume.UI.Module.Common;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class SummonSkillsCell : GridCell<SummonSkillsCell.Model, SummonSkillsScroll.ContextModel>
    {
        public class Model
        {
            public SummonDetailCell.Model SummonDetailCellModel;
            public SkillSheet.Row SkillRow;
            public EquipmentItemOptionSheet.Row OptionRow;
        }

        [SerializeField]
        private SummonDetailCell summonDetailCell;

        [SerializeField]
        private SkillPositionTooltip skillView;

        public override void UpdateContent(Model itemData)
        {
            summonDetailCell.UpdateContent(itemData.SummonDetailCellModel);

            skillView.Show(itemData.SkillRow, itemData.OptionRow);
        }
    }
}
