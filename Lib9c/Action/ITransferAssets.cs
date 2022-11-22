#nullable enable
using System.Collections.Generic;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Action
{
    public interface ITransferAssets
    {
        Address Sender { get; }
        Dictionary<Address, FungibleAssetValue> Map { get; }
        string? Memo { get; }
    }
}
