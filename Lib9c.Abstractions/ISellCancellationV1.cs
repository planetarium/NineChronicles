using System;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface ISellCancellationV1
    {
        Guid ProductId { get; }
        Address SellerAvatarAddress { get; }
    }
}
