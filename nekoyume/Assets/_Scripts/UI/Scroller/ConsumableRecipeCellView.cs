using Nekoyume.Model.Item;
using Nekoyume.TableData;
using System;
using System.Linq;
using Nekoyume.Model.State;

namespace Nekoyume.UI.Scroller
{
    public class ConsumableRecipeCellView : RecipeCellView
    {
        public ConsumableItemRecipeSheet.Row RowData { get; private set; }
        public int UnlockStage { get; private set; }

        public void Set(ConsumableItemRecipeSheet.Row recipeRow)
        {
            if (recipeRow is null)
                return;

            UnlockStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
            var sheet = Game.Game.instance.TableSheets.ConsumableItemSheet;
            if (!sheet.TryGetValue(recipeRow.ResultConsumableItemId, out var row))
                return;

            RowData = recipeRow;

            var consumable = (Consumable)ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Set(consumable);

            StatType = consumable.MainStat;

            var optionString = $"{consumable.MainStat} +{consumable.Stats.First(stat => stat.StatType == consumable.MainStat).ValueAsInt}";
            optionText.text = optionString;
            SetLocked(false, UnlockStage);
        }

        public void Set(AvatarState avatarState)
        {
            if (RowData is null)
                return;

            // 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(UnlockStage))
            {
                SetLocked(true, UnlockStage);
                return;
            }

            SetLocked(false, UnlockStage);

            //재료 검사.
            var inventory = avatarState.inventory;
            SetDimmed(!RowData.MaterialItemIds.All(itemId => inventory.HasItem(itemId)));
        }

    }
}
