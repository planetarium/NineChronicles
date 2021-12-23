using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.State;

namespace Nekoyume.ActionExtensions
{
    public static class RankingBattleExtensions
    {
        public static void PayCost(
            this RankingBattle action,
            IAgent agent,
            States states,
            TableSheets tableSheets,
            IReadOnlyList<Address> updatedAddresses = null,
            bool ignoreNotify = false)
        {
            // NOTE: ignore now
            return;
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (states is null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (tableSheets is null)
            {
                throw new ArgumentNullException(nameof(tableSheets));
            }

            var currentAvatarState = states.CurrentAvatarState;
            if (action.avatarAddress != currentAvatarState.address)
            {
                return;
            }

            // NOTE: arenaInfo.DailyChallengeCount has private setter
            // var weeklyArenaState = states.WeeklyArenaState;
            // if (weeklyArenaState is null ||
            //     !weeklyArenaState.ContainsKey(currentAvatarState.address))
            // {
            //     return;
            // }
            //
            // var arenaInfo = weeklyArenaState[action.avatarAddress];
            // arenaInfo.DailyChallengeCount--;

            ReactiveAvatarState.UpdateActionPoint(currentAvatarState.actionPoint);
            ReactiveAvatarState.UpdateInventory(currentAvatarState.inventory);
        }
    }
}
