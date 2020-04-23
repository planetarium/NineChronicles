using Nekoyume.Model.Item;
using Nekoyume.TableData;
using System;
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
            var sb = new StringBuilder();
            foreach (var stat in consumable.Data.Stats)
            {
                sb.AppendLine($"{stat.StatType} +{stat.Value}");
            }
            optionText.text = sb.ToString();
        }
    }
}
