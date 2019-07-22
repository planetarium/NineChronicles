using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
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

        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>
            {
                ["gold1"] = gold1.ToString(CultureInfo.InvariantCulture),
                ["gold2"] = gold2.ToString(CultureInfo.InvariantCulture),
                ["gold3"] = gold3.ToString(CultureInfo.InvariantCulture),
                ["agentAddresses"] = ByteSerializer.Serialize(agentAddresses),
            }.ToImmutableDictionary();

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            gold1 = decimal.Parse(plainValue["gold1"].ToString());
            gold2 = decimal.Parse(plainValue["gold2"].ToString());
            gold3 = decimal.Parse(plainValue["gold3"].ToString());
            agentAddresses = ByteSerializer.Deserialize<Address[]>((byte[]) plainValue["agentAddresses"]);
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = agentAddresses.Aggregate(states, (current, address) => current.SetState(address, MarkChanged));
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var ranking = (RankingState) states.GetState(RankingState.Address) ?? new RankingState();
            var rewards = new List<decimal>
            {
                gold1,
                gold2,
                gold3
            };

            for (var index = 0; index < agentAddresses.Length; index++)
            {
                var address = agentAddresses[index];
                try
                {
                    var agentState = (AgentState) states.GetState(address);
                    agentState.gold += rewards[index];
                    states = states.SetState(address, agentState);
                }
                catch (InvalidCastException)
                {
                }
                catch (IndexOutOfRangeException)
                {
                    break;
                }
            }

            return states;
        }
    }
}
