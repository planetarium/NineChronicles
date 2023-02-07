#nullable enable
using System.Collections.Generic;
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Abstractions
{
    public interface ITransferAssetsV1
    {
        Address Sender { get; }
        List<(Address recipient, FungibleAssetValue amount)> Recipients { get; }
        string? Memo { get; }
    }
}
