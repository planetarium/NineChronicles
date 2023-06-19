using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Abstractions
{
    public interface IDeliverToOthersGarages
    {
        Address RecipientAgentAddr { get; }
        IOrderedEnumerable<FungibleAssetValue>? FungibleAssetValues { get; }
        IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? FungibleIdAndCounts { get; }
        string? Memo { get; }
    }
}
