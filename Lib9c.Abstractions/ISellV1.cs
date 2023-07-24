#nullable enable
using System;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Abstractions
{
    public interface ISellV1
    {
        Address SellerAvatarAddress { get; }
        Guid ItemId { get; }
        FungibleAssetValue Price { get; }
        string? ItemSubType => null;
    }
}
