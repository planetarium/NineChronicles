using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action
{
    public interface IClaimItems
    {
        public List<(Address address, List<FungibleAssetValue> fungibleAssetValues)> ClaimData { get; }
    }
}
