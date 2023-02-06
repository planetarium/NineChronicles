using System;
using Libplanet;

namespace Nekoyume.Action
{
    public interface ISellCancellationV1
    {
        Guid ProductId { get; }
        Address SellerAvatarAddress { get; }
    }
}
