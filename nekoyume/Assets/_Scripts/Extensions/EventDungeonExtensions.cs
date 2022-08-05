using Nekoyume.L10n;
using Nekoyume.TableData.Event;

namespace Nekoyume
{
    public static class EventDungeonExtensions
    {
        public static string GetLocalizedName(this EventDungeonSheet.Row row) =>
            L10nManager.Localize($"EVENT_DUNGEON_NAME_{row.Id}");
    }
}
