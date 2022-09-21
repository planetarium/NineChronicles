using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Exceptions;
using Nekoyume.Model.Event;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;

namespace Nekoyume.Extensions
{
    public static class EventDungeonExtensions
    {
        public static EventDungeonSheet.Row ValidateFromAction(
            this EventDungeonSheet sheet,
            int eventDungeonId,
            int eventDungeonStageId,
            string actionTypeText,
            string addressesHex)
        {
            if (!sheet.TryGetValue(eventDungeonId, out var row))
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventDungeonId),
                    " Aborted because the event dungeon is not found.",
                    new SheetRowNotFoundException(
                        addressesHex,
                        sheet.Name,
                        eventDungeonId));
            }

            if (eventDungeonStageId < row.StageBegin ||
                eventDungeonStageId > row.StageEnd)
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventDungeonStageId),
                    $"Aborted as the event dungeon stage id({eventDungeonStageId})" +
                    " is out of the range of the event dungeon" +
                    $"({row.StageBegin} ~ {row.StageEnd}).");
            }

            return row;
        }

        public static EventDungeonStageSheet.Row ValidateFromAction(
            this EventDungeonStageSheet sheet,
            int eventDungeonStageId,
            string actionTypeText,
            string addressesHex)
        {
            if (!sheet.TryGetValue(eventDungeonStageId, out var row))
            {
                throw new InvalidActionFieldException(
                    actionTypeText,
                    addressesHex,
                    nameof(eventDungeonStageId),
                    eventDungeonStageId.ToString(CultureInfo.InvariantCulture),
                    new SheetRowNotFoundException(
                        addressesHex,
                        sheet.Name,
                        eventDungeonStageId));
            }

            return row;
        }

        public static int ToEventDungeonStageNumber(this int eventDungeonStageId)
        {
            if (eventDungeonStageId < 10_000_000 ||
                eventDungeonStageId > 99_999_999)
            {
                throw new ArgumentException(
                    $"{nameof(eventDungeonStageId)}({eventDungeonStageId}) must be" +
                    " between 10,000,000 and 99,999,999.");
            }

            return eventDungeonStageId % 10_000;
        }

        public static bool TryGetRowByEventScheduleId(
            this EventDungeonSheet sheet,
            int eventScheduleId,
            out EventDungeonSheet.Row row)
        {
            if (sheet is null)
            {
                row = null;
                return false;
            }

            row = sheet.OrderedList.FirstOrDefault(row =>
                row.Id.ToEventScheduleId() == eventScheduleId);
            return row != null;
        }

        public static bool TryGetRowByEventDungeonStageId(
            this EventDungeonSheet sheet,
            int eventDungeonStageId,
            out EventDungeonSheet.Row row)
        {
            if (sheet is null)
            {
                row = null;
                return false;
            }

            foreach (var dungeonRow in sheet.OrderedList)
            {
                if (eventDungeonStageId < dungeonRow.StageBegin || eventDungeonStageId > dungeonRow.StageEnd)
                {
                    continue;
                }

                row = dungeonRow;
                return true;
            }

            row = null;
            return false;
        }

        public static List<EventDungeonStageSheet.Row> GetStageRows(
            this EventDungeonStageSheet sheet,
            int beginningStageId,
            int endStageId)
        {
            if (sheet is null)
            {
                return new List<EventDungeonStageSheet.Row>();
            }

            return sheet.OrderedList
                .Where(row =>
                    row.Id >= beginningStageId &&
                    row.Id <= endStageId)
                .OrderBy(row => row.Id)
                .ToList();
        }

        public static int GetStageNumber(this EventDungeonStageSheet.Row row) =>
            row.Id.ToEventDungeonStageNumber();

        public static List<EventDungeonStageWaveSheet.Row> GetStageWaveRows(
            this EventDungeonStageWaveSheet sheet,
            int beginningStageId,
            int endStageId)
        {
            if (sheet is null)
            {
                return new List<EventDungeonStageWaveSheet.Row>();
            }

            return sheet.OrderedList
                .Where(row =>
                    row.StageId >= beginningStageId &&
                    row.StageId <= endStageId)
                .OrderBy(row => row.StageId)
                .ToList();
        }

        public static int GetRemainingTicketsConsiderReset(
            this EventDungeonInfo info,
            EventScheduleSheet.Row eventScheduleRow,
            long currentBlockIndex)
        {
            if (eventScheduleRow is null)
            {
                return 0;
            }

            if (info is null)
            {
                if (currentBlockIndex >= eventScheduleRow.StartBlockIndex &&
                    currentBlockIndex <= eventScheduleRow.DungeonEndBlockIndex)
                {
                    return eventScheduleRow.DungeonTicketsMax;
                }

                return 0;
            }

            var blockRange = currentBlockIndex - eventScheduleRow.StartBlockIndex;
            if (blockRange <= 0)
            {
                return 0;
            }

            var interval =
                (int)(blockRange / eventScheduleRow.DungeonTicketsResetIntervalBlockRange);
            return interval > info.ResetTicketsInterval
                ? eventScheduleRow.DungeonTicketsMax
                : info.RemainingTickets;
        }
    }
}
