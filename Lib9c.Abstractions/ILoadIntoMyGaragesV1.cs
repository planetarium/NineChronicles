#nullable enable

using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Abstractions
{
    public interface ILoadIntoMyGaragesV1
    {
        IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>? FungibleAssetValues
        {
            get;
        }

        Address? InventoryAddr { get; }
        IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? FungibleIdAndCounts { get; }
        string? Memo { get; }
    }
}
