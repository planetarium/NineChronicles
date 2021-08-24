using System;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Action
{
    public interface IPurchaseInfo
    {
        Address SellerAgentAddress { get; }
        Address SellerAvatarAddress { get; }
        FungibleAssetValue Price { get; }
    }
}