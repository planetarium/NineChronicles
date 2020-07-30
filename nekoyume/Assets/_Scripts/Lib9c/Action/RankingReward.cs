using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("ranking_reward")]
    public class RankingReward : ActionBase
    {
        public BigInteger gold1;
        public BigInteger gold2;
        public BigInteger gold3;
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
            gold1 = dict["gold1"].ToBigInteger();
            gold2 = dict["gold2"].ToBigInteger();
            gold3 = dict["gold3"].ToBigInteger();
            agentAddresses = dict["agentAddresses"].ToArray(StateExtensions.ToAddress);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.MarkBalanceChanged(GoldCurrencyMock, agentAddresses);
            }

            if (ctx.Signer != ctx.Miner)
            {
                return states;
            }

            var rewards = new List<BigInteger>
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

                BigInteger reward;
                try
                {
                    reward = rewards[index];
                }
                catch (IndexOutOfRangeException)
                {
                    break;
                }

                // FIXME: RankingBattle 액션에서 입장료 받아다 WeeklyArenaAddress에다 쌓아두는데 그거 빼서 주면 안되는지?
                states = states.TransferAsset(
                    GoldCurrencyState.Address,
                    address,
                    states.GetGoldCurrency(),
                    reward
                );
            }

            return states;
        }
    }
}
