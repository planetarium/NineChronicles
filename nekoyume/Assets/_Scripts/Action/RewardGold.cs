using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public Address agentAddress;
        public int gold;

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
            ["agentAddress"] = agentAddress.ToByteArray(),
            ["gold"] = gold.ToString(),
        }.ToImmutableDictionary();
        
        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            agentAddress = new Address((byte[]) plainValue["agentAddress"]);
            gold = int.Parse(plainValue["gold"].ToString());
        }

        public override IAccountStateDelta Execute(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;

            if (actionCtx.Rehearsal)
            {
                return states.SetState(actionCtx.Miner, MarkChanged);
            }
            
            var agentState = (AgentState) states.GetState(actionCtx.Signer) ?? new AgentState(agentAddress);

            agentState.gold += gold;

            return states.SetState(actionCtx.Miner, agentState);
        }
    }
}
