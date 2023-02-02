using System.Collections.Generic;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Action
{
    public interface IPrepareRewardAssetsV1
    {
        Address RewardPoolAddress { get; }
        IEnumerable<FungibleAssetValue> Assets { get; }
    }
}
