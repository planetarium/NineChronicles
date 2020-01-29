using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [ActionType("ranking_reward")]
    public class RankingReward : ActionBase
    {
        public decimal gold1;
        public decimal gold2;
        public decimal gold3;
        public Address[] agentAddresses;

        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "gold1"] = gold1.Serialize(),
                [(Text) "gold2"] = gold2.Serialize(),
                [(Text) "gold3"] = gold3.Serialize(),
                [(Text) "agentAddresses"] = agentAddresses.Select(a => a.Serialize()).Serialize(),
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary) plainValue;
            gold1 = dict["gold1"].ToDecimal();
            gold2 = dict["gold2"].ToDecimal();
            gold3 = dict["gold3"].ToDecimal();
            agentAddresses = dict["agentAddresses"].ToArray(StateExtensions.ToAddress);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = agentAddresses.Aggregate(states, (current, address) => current.SetState(address, MarkChanged));
                return states.SetState(ctx.Miner, MarkChanged);
            }

            if (ctx.Signer != ctx.Miner)
            {
                return states;
            }

            var rewards = new List<decimal>
            {
                gold1,
                gold2,
                gold3
            };

            for (var index = 0; index < agentAddresses.Length; index++)
            {
                var address = agentAddresses[index];
                AgentState agentState = states.GetAgentState(address);
                if (agentState is null)
                {
                    continue;
                }
                try
                {
                    agentState.gold += rewards[index];
                }
                catch (IndexOutOfRangeException)
                {
                    break;
                }

                states = states.SetState(address, agentState.Serialize());
            }

            return states;
        }
    }
}
