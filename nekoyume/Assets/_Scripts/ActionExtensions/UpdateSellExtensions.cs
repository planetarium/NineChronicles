using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.State;

namespace Nekoyume.ActionExtensions
{
    public static class UpdateSellExtensions
    {
        public static void PayCost(
            this UpdateSell action,
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
