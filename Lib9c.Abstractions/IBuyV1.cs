using System;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IBuyV1
    {
        Address BuyerAvatarAddress { get; }
        Address SellerAgentAddress { get; }
        Address SellerAvatarAddress { get; }
        Guid ProductId { get; }
    }
}
