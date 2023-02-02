using System;
using Libplanet;

namespace Nekoyume.Action
{
    public interface ISellCancellationV2
    {
        Guid ProductId { get; }
        Address SellerAvatarAddress { get; }
        string ItemSubType { get; }
    }
}
