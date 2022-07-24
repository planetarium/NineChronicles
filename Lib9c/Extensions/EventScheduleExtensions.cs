using System;
using System.Globalization;
using System.Linq;
using Nekoyume.Exceptions;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;

namespace Nekoyume.Extensions
{
    public static class EventScheduleExtensions
    {
        public static int ToEventScheduleId(this int eventDungeonOrRecipeId)
        {
            if (eventDungeonOrRecipeId < 10_000_000 ||
                eventDungeonOrRecipeId > 99_999_999)
            {
                throw new ArgumentException(
                    $"{nameof(eventDungeonOrRecipeId)}({eventDungeonOrRecipeId}) must be" +
                    " between 10,000,000 and 99,999,999.");
            }

            return eventDungeonOrRecipeId / 10_000;
        }

        public static EventScheduleSheet.Row ValidateFromAction(
            this EventScheduleSheet scheduleSheet,
            long blockIndex,
            int eventScheduleId,
            int eventDungeonOrRecipeId,
            string actionTypeText,
            string addressesHex)
        {
            if (!scheduleSheet.TryGetValue(eventScheduleId, out var scheduleRow))
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventScheduleId),
                    eventScheduleId.ToString(CultureInfo.InvariantCulture),
                    new SheetRowNotFoundException(
                        addressesHex,
                        scheduleSheet.Name,
                        eventScheduleId));
            }

            if (blockIndex < scheduleRow.StartBlockIndex ||
                blockIndex > scheduleRow.DungeonEndBlockIndex)
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventScheduleId),
                    $"Aborted as the block index({blockIndex}) is" +
                    " out of the range of the event schedule" +
                    $"({scheduleRow.StartBlockIndex} ~ {scheduleRow.DungeonEndBlockIndex}).");
            }

            try
            {
                var derivedEventScheduleId =
                    eventDungeonOrRecipeId.ToEventScheduleId();
                if (derivedEventScheduleId != eventScheduleId)
                {
                    throw new InvalidActionFieldException(
                        actionTypeText,
                        addressesHex,
                        nameof(eventDungeonOrRecipeId),
                        "Aborted as the derived" +
                        $" event schedule id({derivedEventScheduleId})" +
                        $" from event dungeon or recipe id({eventDungeonOrRecipeId})" +
                        $" is not matched with the event schedule id({eventScheduleId}).");
                }
            }
            catch (ArgumentException e)
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventDungeonOrRecipeId),
                    eventDungeonOrRecipeId.ToString(CultureInfo.InvariantCulture),
                    e);
            }

            return scheduleRow;
        }

        public static bool TryGetRowForDungeon(
            this EventScheduleSheet sheet,
            long blockIndex,
            out EventScheduleSheet.Row row)
        {
            if (sheet is null)
            {
                row = null;
                return false;
            }

            row = sheet.OrderedList.FirstOrDefault(row =>
                row.StartBlockIndex <= blockIndex &&
                row.DungeonEndBlockIndex >= blockIndex);
            return row != null;
        }

        public static bool TryGetRowForRecipe(
            this EventScheduleSheet sheet,
            long blockIndex,
            out EventScheduleSheet.Row row)
        {
            if (sheet is null)
            {
                row = null;
                return false;
            }

            row = sheet.OrderedList.FirstOrDefault(row =>
                row.StartBlockIndex <= blockIndex &&
                row.RecipeEndBlockIndex >= blockIndex);
            return row != null;
        }
    }
}
