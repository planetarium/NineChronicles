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
            public EquipmentItemOptionSheet.Row EquipmentOptionRow;
            public RuneOptionSheet.Row.RuneOptionInfo RuneOptionInfo;
        }

        [SerializeField]
        private SummonDetailCell summonDetailCell;

        [SerializeField]
        private SkillPositionTooltip skillView;

        public override void UpdateContent(Model itemData)
        {
            summonDetailCell.UpdateContent(itemData.SummonDetailCellModel);

            if (itemData.EquipmentOptionRow is not null)
            {
                skillView.Show(itemData.SkillRow, itemData.EquipmentOptionRow);
            }

            if (itemData.RuneOptionInfo is not null)
            {
                skillView.Show(itemData.SkillRow, itemData.RuneOptionInfo);
            }
        }
    }
}
