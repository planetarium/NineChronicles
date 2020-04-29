using Nekoyume.Model.Item;
using Nekoyume.TableData;
using System;
using System.Linq;
using System.Text;

namespace Nekoyume.UI.Scroller
{
    public class ConsumableRecipeCellView : RecipeCellView
    {
        public ConsumableItemRecipeSheet.Row RowData { get; private set; }

        public void Set(ConsumableItemRecipeSheet.Row recipeRow)
        {
            if (recipeRow is null)
                return;

            var sheet = Game.Game.instance.TableSheets.ConsumableItemSheet;
            if (!sheet.TryGetValue(recipeRow.ResultConsumableItemId, out var row))
                return;

            RowData = recipeRow;

            var consumable = (Consumable)ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Set(consumable);

            StatType = consumable.MainStat;

            var optionString = $"{consumable.MainStat} +{consumable.Data.Stats.First(stat => stat.StatType == consumable.MainStat).ValueAsInt}";
            optionText.text = optionString;
        }
    }
}
