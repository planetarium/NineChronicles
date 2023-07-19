using System.Linq;
using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Abstractions
{
    public interface IDeliverToOthersGaragesV1
    {
        Address RecipientAgentAddr { get; }
        IOrderedEnumerable<FungibleAssetValue>? FungibleAssetValues { get; }
        IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? FungibleIdAndCounts { get; }
        string? Memo { get; }
    }
}
