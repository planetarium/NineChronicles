using System.Collections.Generic;
using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossRankItems
    {
        public List<WorldBossRankItem> UserItems { get; }
        public WorldBossRankItem MyItem { get; }
        public readonly int UserCount;

        public WorldBossRankItems(
            List<WorldBossRankItem> userItems,
            WorldBossRankItem myItem,
            int userCount)
        {
            UserItems = userItems;
            MyItem = myItem;
            UserCount = userCount;
        }
    }
}
