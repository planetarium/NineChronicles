using System;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface ISellCancellationV3
    {
        Guid OrderId { get; }
        Guid TradableId { get; }
        Address SellerAvatarAddress { get; }
        string ItemSubType { get; }
    }
}
