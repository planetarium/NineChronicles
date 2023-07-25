using System.Collections.Generic;
using Libplanet.Action;
using Libplanet.Crypto;

namespace Nekoyume.Action
{
    /// <summary>
    /// Common interface used after <see cref="IBuy5"/>.
    /// </summary>
    /// <seealso cref="IBuy5"/>
    public interface IBuy5 : IAction
    {
        Address buyerAvatarAddress { get; }
        IEnumerable<IPurchaseInfo> purchaseInfos { get; }
    }
}
