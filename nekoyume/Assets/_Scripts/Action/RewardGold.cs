using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public int gold;

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            gold = int.Parse(plainValue["gold"].ToString());
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            IAccountStateDelta states = actionCtx.PreviousStates;

            var agentState = (AgentState) states.GetState(actionCtx.Signer) ?? new AgentState();

            if (actionCtx.Rehearsal)
            {
                return states.SetState(actionCtx.Miner, agentState);
            }

            agentState.gold += gold;

            return states.SetState(actionCtx.Miner, agentState);
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
            ["gold"] = gold.ToString(),
        }.ToImmutableDictionary();
    }
}
