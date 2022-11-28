#nullable enable
using System.Collections.Generic;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Action
{
    public interface ITransferAssets
    {
        Address Sender { get; }
        List<(Address recipient, FungibleAssetValue amount)> Recipients { get; }
        string? Memo { get; }
    }
}
