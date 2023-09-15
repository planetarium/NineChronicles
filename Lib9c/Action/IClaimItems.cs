using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action
{
    public interface IClaimItems
    {
        Address AvatarAddress { get; }
        IEnumerable<FungibleAssetValue> Amounts { get; }
    }
}
