using System.Linq;
using Nekoyume.Model.Stat;
using Nekoyume.TableData.Summon;
using Nekoyume.UI.Scroller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SummonSkillsPopup : PopupWidget
    {
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SummonSkillsScroll scroll;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() => Close());
            CloseWidget = closeButton.onClick.Invoke;
        }

        public void Show(SummonSheet.Row summonRow, bool ignoreShowAnimation = false)
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var recipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var subRecipeSheet = tableSheets.EquipmentItemSubRecipeSheetV2;
            var equipmentSheet = tableSheets.EquipmentItemSheet;
            var skillSheet = tableSheets.SkillSheet;
            var optionSheet = tableSheets.EquipmentItemOptionSheet;

            var models = summonRow.Recipes.Where(pair => pair.Item1 > 0).Select(pair =>
            {
                var (recipeId, ratio) = pair;

                if (!recipeSheet.TryGetValue(recipeId, out var recipeRow))
                {
                    return null;
                }

                var optionRow = subRecipeSheet[recipeRow.SubRecipeIds[0]].Options
                    .Select(optionInfo => optionSheet[optionInfo.Id])
                    .FirstOrDefault(optionRow => optionRow.StatType == StatType.NONE);
                if (optionRow == null)
                {
                    return null;
                }

                var skillRow = skillSheet[optionRow.SkillId];
                return new SummonSkillsCell.Model
                {
                    SummonDetailCellModel = new SummonDetailCell.Model
                    {
                        EquipmentRow = equipmentSheet[recipeRow.ResultEquipmentId],
                        Ratio = ratio,
                    },
                    SkillRow = skillRow,
                    OptionRow = optionRow,
                };
            }).Where(model => model != null).OrderBy(model => model.SummonDetailCellModel.Ratio);
            scroll.UpdateData(models, true);

            base.Show(ignoreShowAnimation);
        }
    }
}
