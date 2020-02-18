using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("weekly_arena_reward")]
    public class WeeklyArenaReward : GameAction
    {
        public Address AvatarAddress;
        public Address WeeklyArenaAddress;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Signer, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState, out _))
            {
                return states;
            }

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);
            if (weeklyArenaState is null)
            {
                return states;
            }

            if (!weeklyArenaState.TryGetValue(AvatarAddress, out var info))
            {
                return states;
            }

            if (info.Receive)
            {
                return states;
            }

            info.Receive = true;

            var tier = weeklyArenaState.GetTier(info);

            var gold = weeklyArenaState.GetReward(tier);

            agentState.gold += gold;

            return states.SetState(ctx.Signer, agentState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["avatarAddress"] = AvatarAddress.Serialize(),
            ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize()
        }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
        }
    }
}
