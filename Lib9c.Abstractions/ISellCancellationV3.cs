using System;
using Libplanet.Crypto;

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
