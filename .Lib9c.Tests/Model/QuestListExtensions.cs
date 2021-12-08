namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Quest;

    public static class QuestListExtensions
    {
        public static IEnumerable<T> OfType<T>(this QuestList quests)
            where T : Quest
        =>
            quests.EnumerateLazyQuestStates()
                .Select(l => l.State)
                .OfType<T>()
                .OrderBy(q => q.Id);
    }
}
