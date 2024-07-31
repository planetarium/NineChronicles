using System.Linq;
using Nekoyume.Model.Item;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI
{
    public class CustomEquipmentSkillPopup : SummonSkillsPopup
    {
        public void Show(ItemSubType subType)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var skillSheet = tableSheets.SkillSheet;
            var equipmentItemOptionSheet = tableSheets.EquipmentItemOptionSheet;

            var models = tableSheets.CustomEquipmentCraftRecipeSkillSheet.Values
                .Where(row => row.ItemSubType == subType)
                .Select(row =>
                {
                    var optionRow = equipmentItemOptionSheet[row.ItemOptionId];
                    var skillRow = skillSheet[optionRow.SkillId];
                    return new SummonSkillsCell.Model
                    {
                        SkillRow = skillRow,
                        EquipmentOptionRow = optionRow,
                    };
                });
            scroll.UpdateData(models, true);

            base.Show();
        }
    }
}
