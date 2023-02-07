using System;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface ISellCancellationV1
    {
        Guid ProductId { get; }
        Address SellerAvatarAddress { get; }
    }
}
