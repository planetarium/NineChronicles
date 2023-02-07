using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Lib9c.Abstractions
{
    public interface IBuyV2
    {
        Address BuyerAvatarAddress { get; }
        IEnumerable<IValue> PurchaseInfos { get; }
    }
}
