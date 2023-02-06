using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IBuyMultipleV1
    {
        Address BuyerAvatarAddress { get; }
        IEnumerable<IValue> PurchaseInfos { get; }
    }
}
