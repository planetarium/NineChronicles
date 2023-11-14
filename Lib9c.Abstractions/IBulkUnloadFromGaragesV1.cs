using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Abstractions
{
    public interface IBulkUnloadFromGaragesV1
    {
        public IReadOnlyList<(
            Address recipientAvatarAddress,
            IOrderedEnumerable<(Address balanceAddress, FungibleAssetValue value)>?
            fungibleAssetValues,
            IOrderedEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo)> UnloadData { get; }
    }
}
