using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
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
            var equipmentItemRecipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var equipmentItemSubRecipeSheet = tableSheets.EquipmentItemSubRecipeSheetV2;
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var skillSheet = tableSheets.SkillSheet;
            var equipmentItemOptionSheet = tableSheets.EquipmentItemOptionSheet;
            var runeSheet = tableSheets.RuneSheet;
            var runeOptionSheet = tableSheets.RuneOptionSheet;

            var models = summonRow.Recipes.Where(pair => pair.Item1 > 0).Select(pair =>
            {
                var (recipeId, ratio) = pair;

                EquipmentItemSheet.Row equipmentRow = null;
                List<EquipmentItemSubRecipeSheetV2.OptionInfo> optionInfos = null;
                EquipmentItemOptionSheet.Row equipmentOptionRow = null;
                SkillSheet.Row skillRow = null;
                if (equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
                {
                    equipmentRow = equipmentItemSheet[recipeRow.ResultEquipmentId];
                    optionInfos = equipmentItemSubRecipeSheet[recipeRow.SubRecipeIds[0]].Options;
                    equipmentOptionRow = optionInfos
                        .Select(optionInfo => equipmentItemOptionSheet[optionInfo.Id])
                        .First(optionRow => optionRow.StatType == StatType.NONE);
                    skillRow = skillSheet[equipmentOptionRow.SkillId];
                }

                string runeTicker = null;
                RuneOptionSheet.Row.RuneOptionInfo runeOptionInfo = null;
                if (runeSheet.TryGetValue(recipeId, out var runeRow))
                {
                    runeTicker = runeRow.Ticker;

                    if (runeOptionSheet.TryGetValue(runeRow.Id, out var runeOptionRow) &&
                        runeOptionRow.LevelOptionMap.TryGetValue(1, out runeOptionInfo))
                    {
                        if (runeOptionInfo.SkillId == 0)
                        {
                            return null;
                        }

                        skillRow = skillSheet[runeOptionInfo.SkillId];
                    }
                }

                return new SummonSkillsCell.Model
                {
                    SummonDetailCellModel = new SummonDetailCell.Model
                    {
                        EquipmentRow = equipmentRow,
                        Options = optionInfos,
                        RuneTicker = runeTicker,
                        Ratio = ratio
                    },
                    SkillRow = skillRow,
                    EquipmentOptionRow = equipmentOptionRow,
                    RuneOptionInfo = runeOptionInfo
                };
            }).Where(model => model != null).OrderBy(model => model.SummonDetailCellModel.Ratio);
            scroll.UpdateData(models, true);

            base.Show(ignoreShowAnimation);
        }
    }
}
