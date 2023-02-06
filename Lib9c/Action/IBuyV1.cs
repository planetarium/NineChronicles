using System;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IBuyV1
    {
        Address BuyerAvatarAddress { get; }
        Address SellerAgentAddress { get; }
        Address SellerAvatarAddress { get; }
        Guid ProductId { get; }
    }
}
