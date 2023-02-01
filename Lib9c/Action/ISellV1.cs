#nullable enable
using System;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Action
{
    public interface ISellV1
    {
        Address SellerAvatarAddress { get; }
        Guid ItemId { get; }
        FungibleAssetValue Price { get; }
        string? ItemSubType => null;
    }
}
