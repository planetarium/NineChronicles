using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Exceptions;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;

namespace Nekoyume.Extensions
{
    public static class EventRecipeExtensions
    {
        public static EventConsumableItemRecipeSheet.Row ValidateFromAction(
            this EventConsumableItemRecipeSheet sheet,
            int eventConsumableItemRecipeId,
            string actionTypeText,
            string addressesHex)
        {
            if (!sheet.TryGetValue(eventConsumableItemRecipeId, out var row))
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventConsumableItemRecipeId),
                    " Aborted because the event recipe is not found.",
                    new SheetRowNotFoundException(
                        addressesHex,
                        sheet.Name,
                        eventConsumableItemRecipeId));
            }

            return row;
        }

        public static List<EventConsumableItemRecipeSheet.Row> GetRecipeRows(
            this EventConsumableItemRecipeSheet sheet,
            int eventScheduleId)
        {
            if (sheet is null)
            {
                return new List<EventConsumableItemRecipeSheet.Row>();
            }

            return sheet.OrderedList
                .Where(row => row.Id.ToEventScheduleId() == eventScheduleId)
                .OrderBy(row => row.Id)
                .ToList();
        }

        public static EventMaterialItemRecipeSheet.Row ValidateFromAction(
            this EventMaterialItemRecipeSheet sheet,
            int eventMaterialItemRecipeId,
            string actionTypeText,
            string addressesHex)
        {
            if (!sheet.TryGetValue(eventMaterialItemRecipeId, out var row))
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventMaterialItemRecipeId),
                    " Aborted because the event recipe is not found.",
                    new SheetRowNotFoundException(
                        addressesHex,
                        sheet.Name,
                        eventMaterialItemRecipeId));
            }

            return row;
        }

        public static void ValidateFromAction(
            this EventMaterialItemRecipeSheet.Row recipeRow,
            MaterialItemSheet materialItemSheet,
            Dictionary<int, int> materialsToUse,
            string actionTypeText,
            string addressesHex
        )
        {
            var materialsCount = 0;
            foreach (var pair in materialsToUse.OrderBy(pair => pair.Key))
            {
                if (!materialItemSheet.TryGetValue(pair.Key, out _))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(MaterialItemSheet),
                        pair.Key);
                }

                if (recipeRow.RequiredMaterialsId.Contains(pair.Key))
                {
                    materialsCount += pair.Value;
                }
            }

            if (recipeRow.RequiredMaterialsCount != materialsCount)
            {
                throw new InvalidMaterialCountException(
                    actionTypeText,
                    addressesHex,
                    recipeRow.RequiredMaterialsCount,
                    materialsCount);
            }
        }

        public static List<EventMaterialItemRecipeSheet.Row> GetRecipeRows(
            this EventMaterialItemRecipeSheet sheet,
            int eventScheduleId)
        {
            if (sheet is null)
            {
                return new List<EventMaterialItemRecipeSheet.Row>();
            }

            return sheet.OrderedList
                .Where(row => row.Id.ToEventScheduleId() == eventScheduleId)
                .OrderBy(row => row.Id)
                .ToList();
        }
    }
}
