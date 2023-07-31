#nullable enable
using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Abstractions
{
    public interface ITransferAssetsV1
    {
        Address Sender { get; }
        List<(Address recipient, FungibleAssetValue amount)> Recipients { get; }
        string? Memo { get; }
    }
}
