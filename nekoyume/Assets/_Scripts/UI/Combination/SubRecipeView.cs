using Nekoyume.Game.Controller;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.Helper;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    public class SubRecipeView : MonoBehaviour
    {
        [SerializeField] private List<Toggle> categoryToggles = null;
        [SerializeField] private RecipeCell recipeCell = null;
        [SerializeField] private TextMeshProUGUI titleText = null;
        [SerializeField] private TextMeshProUGUI statText = null;

        [SerializeField] private TextMeshProUGUI blockIndexText = null;
        [SerializeField] private TextMeshProUGUI greatSuccessRateText = null;

        private SheetRow<int> _recipeRow = null;
        private List<int> _subrecipeIds = null;

        private const string StatTextFormat = "{0} {1}";

        private void Awake()
        {
            for (int i = 0; i < categoryToggles.Count; ++i)
            {
                var innerIndex = i;
                var toggle = categoryToggles[i];
                toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    ChangeTab(innerIndex);
                });
            }
        }

        public void SetData(SheetRow<int> recipeRow, List<int> subrecipeIds)
        {
            _subrecipeIds = subrecipeIds;

            string title = null;
            if (recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
            {
                var resultItem = equipmentRow.GetResultItem();
                title = resultItem.GetLocalizedName();

                var stat = resultItem.GetUniqueStat();
                statText.text = string.Format(StatTextFormat, stat.Type, stat.ValueAsInt);
                recipeCell.Show(equipmentRow);
            }
            else if (recipeRow is ConsumableItemRecipeSheet.Row consumableRow)
            {
                var resultItem = consumableRow.GetResultItem();
                title = resultItem.GetLocalizedName();

                var stat = resultItem.GetUniqueStat();
                statText.text = string.Format(StatTextFormat, stat.StatType, stat.ValueAsInt);
                recipeCell.Show(consumableRow);
            }

            titleText.text = title;

            var firstCategoryToggle = categoryToggles.First();
            if (firstCategoryToggle.isOn)
            {
                ChangeTab(0);
            }
            else
            {
                firstCategoryToggle.isOn = true;
            }
        }

        private void ChangeTab(int index)
        {
            long blockIndex;
            decimal greatSuccessRate;

            if (_subrecipeIds.Any())
            {
                var subRecipeId = _subrecipeIds[index];
                var subRecipe = Game.Game.instance.TableSheets
                    .EquipmentItemSubRecipeSheetV2[subRecipeId];
                var options = subRecipe.Options;

                blockIndex = subRecipe.RequiredBlockIndex;
                greatSuccessRate = options
                    .Select(x => x.Ratio)
                    .Aggregate((a, b) => a * b);
            }
            else
            {
                if (_recipeRow is EquipmentItemRecipeSheet.Row equipmentRow)
                {
                    blockIndex = equipmentRow.RequiredBlockIndex;
                }
                else if (_recipeRow is ConsumableItemRecipeSheet.Row consumableRow)
                {
                    blockIndex = consumableRow.RequiredBlockIndex;
                }
                else
                {
                    blockIndex = 0;
                }

                greatSuccessRate = 0m;
            }

            blockIndexText.text = blockIndex.ToString();
            greatSuccessRateText.text = greatSuccessRate.ToString("P1");
        }
    }
}
