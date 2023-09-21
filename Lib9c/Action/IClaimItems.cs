using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action
{
    public interface IClaimItems
    {
        IEnumerable<Address> AvatarAddresses { get; }
        IEnumerable<FungibleAssetValue> Amounts { get; }
    }
}
