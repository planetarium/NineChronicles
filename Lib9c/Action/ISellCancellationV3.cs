using System;
using Libplanet;

namespace Nekoyume.Action
{
    public interface ISellCancellationV3
    {
        Guid OrderId { get; }
        Guid TradableId { get; }
        Address SellerAvatarAddress { get; }
        string ItemSubType { get; }
    }
}
