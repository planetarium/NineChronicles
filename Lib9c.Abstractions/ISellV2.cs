using System;
using Libplanet;
using Libplanet.Assets;

namespace Lib9c.Abstractions
{
    public interface ISellV2
    {
        Address SellerAvatarAddress { get; }
        Guid TradableId { get; }
        int Count { get; }
        FungibleAssetValue Price { get; }
        string ItemSubType { get; }
        Guid? OrderId => null;
    }
}
