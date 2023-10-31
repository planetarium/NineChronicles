using System.Collections.Generic;
using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Abstractions;

public interface IUnloadFromGaragesV1
{
    public IReadOnlyList<(
        Address recipientAvatarAddress,
        IReadOnlyList<(Address balanceAddress, FungibleAssetValue value)>? fungibleAssetValues,
        IReadOnlyList<(HashDigest<SHA256> fungibleId, int count)>? FungibleIdAndCounts)> UnloadData
    {
        get;
    }

    string? Memo { get; }
}
