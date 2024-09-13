using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Model
{
    public class WorldBossRewards
    {
        public readonly List<FungibleAssetValue> Assets = new();
        public readonly Dictionary<TradableMaterial, int> Materials = new();

        public WorldBossRewards()
        {
        }

        public WorldBossRewards(List<FungibleAssetValue> assets, IEnumerable<ItemBase> materials)
        {
            Assets = assets;
            Materials = materials
                .OfType<TradableMaterial>()
                .GroupBy(material => material)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.Count());
        }

        public bool Any()
        {
            return Assets.Count > 0 || Materials.Count > 0;
        }
    }
}
