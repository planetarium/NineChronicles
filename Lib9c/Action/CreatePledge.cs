using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Libplanet.State;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class CreatePledge : ActionBase
    {
        public const string TypeIdentifier = "create_pledge";
        public Address PatronAddress;
        public int Mead;
        public IEnumerable<Address> AgentAddresses;
        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty
                    .Add(PatronAddress.Serialize())
                    .Add(Mead.Serialize())
                    .Add(new List(AgentAddresses.Select(a => a.Serialize()))));
        public override void LoadPlainValue(IValue plainValue)
        {
            var list = (List)((Dictionary)plainValue)["values"];
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
                states = states
                    .TransferAsset(PatronAddress, agentAddress, Mead * Currencies.Mead)
                    .SetState(contractAddress, contractList);
            }

            return states;
        }
    }
}
