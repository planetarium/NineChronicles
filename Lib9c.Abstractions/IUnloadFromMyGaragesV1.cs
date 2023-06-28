#nullable enable

using System.Linq;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Abstractions
{
    public interface IUnloadFromMyGaragesV1
    {
        Address RecipientAvatarAddr { get; }

        IOrderedEnumerable<(Address balanceAddr, FungibleAssetValue value)>? FungibleAssetValues
        {
            get;
        }

        IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? FungibleIdAndCounts { get; }
        string? Memo { get; }
    }
}
