using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Event;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;

namespace Nekoyume.Extensions
{
    public static class EventDungeonExtensions
    {
        public static int ToEventScheduleId(this int eventDungeonId)
        {
            if (eventDungeonId < 10_000_000 ||
                eventDungeonId > 99_999_999)
            {
                throw new ArgumentException(
                    $"{nameof(eventDungeonId)}({eventDungeonId}) must be" +
                    " between 10,000,000 and 99,999,999.");
            }

            return eventDungeonId / 10_000;
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
