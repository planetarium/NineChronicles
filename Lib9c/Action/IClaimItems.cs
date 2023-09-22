using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action
{
    public interface IClaimItems
    {
        List<Address> AvatarAddresses { get; }
        List<FungibleAssetValue> Amounts { get; }
    }
}
