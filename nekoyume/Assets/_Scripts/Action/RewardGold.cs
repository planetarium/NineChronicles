using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>().ToImmutableDictionary();

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Miner, MarkChanged);
            }

            var agentState = (AgentState) states.GetState(ctx.Signer) ?? new AgentState(ctx.Signer);
            agentState.gold += 1;

            return states.SetState(ctx.Miner, agentState);
        }
    }
}
