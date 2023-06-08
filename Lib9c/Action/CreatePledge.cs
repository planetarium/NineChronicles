using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("create_pledge")]
    public class CreatePledge : ActionBase
    {
        public Address PatronAddress;
        public int Mead;
        public IEnumerable<Address> AgentAddresses;
        public override IValue PlainValue => List.Empty
            .Add(PatronAddress.Serialize())
            .Add(Mead.Serialize())
            .Add(new List(AgentAddresses.Select(a => a.Serialize())));
        public override void LoadPlainValue(IValue plainValue)
        {
            var list = (List) plainValue;
            PatronAddress = list[0].ToAddress();
            Mead = list[1].ToInteger();
            AgentAddresses = list[2].ToList(StateExtensions.ToAddress);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousStates;
            var contractList = List.Empty
                .Add(PatronAddress.Serialize())
                .Add(true.Serialize())
                .Add(Mead.Serialize());
            foreach (var agentAddress in AgentAddresses)
            {
                var contractAddress = agentAddress.GetPledgeAddress();
                if (states.TryGetState(contractAddress, out List _))
                {
                    continue;
                }

                states = states
                    .TransferAsset(PatronAddress, agentAddress, Mead * Currencies.Mead)
                    .SetState(contractAddress, contractList);
            }

            return states;
        }
    }
}
