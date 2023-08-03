using System;
using Libplanet.Crypto;

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
