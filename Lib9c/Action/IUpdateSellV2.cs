using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Action
{
    public interface IUpdateSellV2
    {
        Address SellerAvatarAddress { get; }
        IEnumerable<IValue> UpdateSellInfos { get; }
    }
}
