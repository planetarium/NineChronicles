#nullable enable
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Abstractions
{
    public interface ITransferAssetV1
    {
        Address Sender { get; }
        Address Recipient { get; }
        FungibleAssetValue Amount { get; }
        string? Memo { get; }
    }
}
