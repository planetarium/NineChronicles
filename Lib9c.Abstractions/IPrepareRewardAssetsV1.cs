using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Abstractions
{
    public interface IPrepareRewardAssetsV1
    {
        Address RewardPoolAddress { get; }
        IEnumerable<FungibleAssetValue> Assets { get; }
    }
}
