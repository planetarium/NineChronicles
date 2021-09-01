using System;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;

namespace Nekoyume.Action
{
    public interface IPurchaseInfo
    {
        Guid? OrderId { get; }
        Address SellerAgentAddress { get; }
        Address SellerAvatarAddress { get; }
        FungibleAssetValue Price { get; }
        ItemSubType ItemSubType { get; }
    }
}
