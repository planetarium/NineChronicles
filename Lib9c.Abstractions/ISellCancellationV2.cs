using System;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface ISellCancellationV2
    {
        Guid ProductId { get; }
        Address SellerAvatarAddress { get; }
        string ItemSubType { get; }
    }
}
