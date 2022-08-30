using System.Collections.Generic;
using Libplanet;
using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossRankItems
    {
        public List<WorldBossRankItem> UserItems { get; }
        public WorldBossRankItem MyItem { get; }
        public Address AvatarAddress { get; }
        public readonly long LastUpdatedBlockIndex;
        public readonly int UserCount;

        public WorldBossRankItems(
            List<WorldBossRankItem> userItems,
            WorldBossRankItem myItem,
            Address avatarAddress,
            long lastUpdatedBlockIndex,
            int userCount)
        {
            UserItems = userItems;
            MyItem = myItem;
            AvatarAddress = avatarAddress;
            LastUpdatedBlockIndex = lastUpdatedBlockIndex;
            UserCount = userCount;
        }
    }
}
