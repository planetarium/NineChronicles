using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IBuyV2
    {
        Address BuyerAvatarAddress { get; }
        IEnumerable<IValue> PurchaseInfos { get; }
    }
}
