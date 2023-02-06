#nullable enable
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Action
{
    public interface ITransferAssetV1
    {
        Address Sender { get; }
        Address Recipient { get; }
        FungibleAssetValue Amount { get; }
        string? Memo { get; }
    }
}
