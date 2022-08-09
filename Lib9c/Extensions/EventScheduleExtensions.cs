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

        public static int GetStageExp(
            this EventScheduleSheet.Row row,
            int stageNumber,
            int multiplier = 1) =>
            (stageNumber / 11 * row.DungeonExpSeedValue + 1) * multiplier;

        public static EventScheduleSheet.Row ValidateFromActionForDungeon(
            this EventScheduleSheet scheduleSheet,
            long blockIndex,
            int eventScheduleId,
            int eventDungeonId,
            string actionTypeText,
            string addressesHex)
        {
            var scheduleRow = ValidateFromAction(
                scheduleSheet,
                eventScheduleId,
                actionTypeText,
                addressesHex);

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
                    eventDungeonId.ToEventScheduleId();
                if (derivedEventScheduleId != eventScheduleId)
                {
                    throw new InvalidActionFieldException(
                        actionTypeText,
                        addressesHex,
                        nameof(eventDungeonId),
                        "Aborted as the derived" +
                        $" event schedule id({derivedEventScheduleId})" +
                        $" from event dungeon id({eventDungeonId})" +
                        $" is not matched with the event schedule id({eventScheduleId}).");
                }
            }
            catch (ArgumentException e)
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventDungeonId),
                    eventDungeonId.ToString(CultureInfo.InvariantCulture),
                    e);
            }

            return scheduleRow;
        }

        public static EventScheduleSheet.Row ValidateFromActionForRecipe(
            this EventScheduleSheet scheduleSheet,
            long blockIndex,
            int eventScheduleId,
            int eventRecipeId,
            string actionTypeText,
            string addressesHex)
        {
            var scheduleRow = ValidateFromAction(
                scheduleSheet,
                eventScheduleId,
                actionTypeText,
                addressesHex);

            if (blockIndex < scheduleRow.StartBlockIndex ||
                blockIndex > scheduleRow.RecipeEndBlockIndex)
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventScheduleId),
                    $"Aborted as the block index({blockIndex}) is" +
                    " out of the range of the event schedule" +
                    $"({scheduleRow.StartBlockIndex} ~ {scheduleRow.RecipeEndBlockIndex}).");
            }

            try
            {
                var derivedEventScheduleId =
                    eventRecipeId.ToEventScheduleId();
                if (derivedEventScheduleId != eventScheduleId)
                {
                    throw new InvalidActionFieldException(
                        actionTypeText,
                        addressesHex,
                        nameof(eventRecipeId),
                        "Aborted as the derived" +
                        $" event schedule id({derivedEventScheduleId})" +
                        $" from event recipe id({eventRecipeId})" +
                        $" is not matched with the event schedule id({eventScheduleId}).");
                }
            }
            catch (ArgumentException e)
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventRecipeId),
                    eventRecipeId.ToString(CultureInfo.InvariantCulture),
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

        private static EventScheduleSheet.Row ValidateFromAction(
            EventScheduleSheet scheduleSheet,
            int eventScheduleId,
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

            return scheduleRow;
        }
    }
}
