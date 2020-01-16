using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
        public decimal gold;

        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "gold"] = gold.Serialize(),
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary) plainValue;
            gold = dict["gold"].ToDecimal();
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Miner, MarkChanged);
            }

            AgentState agentState = states.GetAgentState(ctx.Signer) ?? new AgentState(ctx.Signer);
            agentState.gold += gold;

            return states.SetState(ctx.Miner, agentState.Serialize());
        }
    }
}
