using System.Collections.Generic;
using Libplanet.Types.Assets;

namespace Nekoyume.UI.Model
{
    public class RuneStoneInventoryItem
    {
        public List<FungibleAssetValue> Runes { get; }
        public int BoosId { get; }

        public RuneStoneInventoryItem(List<FungibleAssetValue> runes, int boosId)
        {
            Runes = runes;
            BoosId = boosId;
        }
    }
}
