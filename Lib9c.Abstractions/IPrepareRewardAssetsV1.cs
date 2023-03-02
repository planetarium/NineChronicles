using System.Collections.Generic;
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Abstractions
{
    public interface IPrepareRewardAssetsV1
    {
        Address RewardPoolAddress { get; }
        IEnumerable<FungibleAssetValue> Assets { get; }
    }
}
