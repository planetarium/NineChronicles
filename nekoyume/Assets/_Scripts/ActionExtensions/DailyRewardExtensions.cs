using System;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.State;

namespace Nekoyume.ActionExtensions
{
    public static class DailyRewardExtensions
    {
        public static void PayCost(this DailyReward action, IAgent agent, States states, TableSheets tableSheets)
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

            var nextBlockIndex = agent.BlockIndex + 1;
            currentAvatarState.dailyRewardReceivedIndex = nextBlockIndex;

            ReactiveAvatarState.UpdateDailyRewardReceivedIndex(currentAvatarState.dailyRewardReceivedIndex);
        }
    }
}
