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
        public decimal gold;
        
        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>
            {
                ["gold"] = gold.ToString(CultureInfo.InvariantCulture),
            }.ToImmutableDictionary();

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            gold = decimal.Parse(plainValue["gold"].ToString());
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Miner, MarkChanged);
            }

            var agentState = (AgentState) states.GetState(ctx.Signer) ?? new AgentState(ctx.Signer);
            agentState.gold += gold;

            return states.SetState(ctx.Miner, agentState);
        }
    }
}
