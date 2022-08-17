using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.TableData.Event;

namespace Nekoyume
{
    public static class EventDungeonExtensions
    {
        public static string GetLocalizedName(this EventDungeonSheet.Row row) =>
            L10nManager.Localize($"EVENT_DUNGEON_NAME_{row.Id}");

        public static List<EventDungeonStageSheet.Row> GetStagesContainsReward(
            this List<EventDungeonStageSheet.Row> rows,
            int itemId)
        {
            if (RxProps.EventDungeonInfo == null)
            {
                return null;
            }

            return rows
                .Where(r => r.Rewards.Any(reward => reward.ItemId == itemId))
                .ToList();
        }
    }
}
