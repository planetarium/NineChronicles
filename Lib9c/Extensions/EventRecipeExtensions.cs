using System.Collections.Generic;
using System.Linq;
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
    }
}
