using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.State;

namespace Nekoyume.ActionExtensions
{
    public static class RedeemCodeExtensions
    {
        public static void PayCost(
            this RedeemCode action,
            IAgent agent,
            States states,
            TableSheets tableSheets,
            IReadOnlyList<Address> updatedAddresses = null,
            bool ignoreNotify = false)
        {
            // NOTE: ignore
        }
    }
}
