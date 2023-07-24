using System;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
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
