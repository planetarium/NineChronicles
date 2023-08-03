using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class RequestPledge : ActionBase
    {
        public const string TypeIdentifier = "request_pledge";

        public RequestPledge()
        {
        }

        // Value from tx per block policy.
        // https://github.com/planetarium/lib9c/blob/b6c1e85abc0b93347dae8e1a12aaefd767b27632/Lib9c.Policy/Policy/MaxTransactionsPerSignerPerBlockPolicy.cs#L29
        public const int DefaultRefillMead = 4;
        public Address AgentAddress;
        public int RefillMead;

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty.Add(AgentAddress.Serialize()).Add(RefillMead.Serialize()));

        public override void LoadPlainValue(IValue plainValue)
        {
            List values = (List)((Dictionary)plainValue)["values"];
            AgentAddress = values[0].ToAddress();
            RefillMead = values[1].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var contractAddress = AgentAddress.GetPledgeAddress();
            if (states.TryGetState(contractAddress, out List _))
            {
                throw new AlreadyContractedException($"{AgentAddress} already contracted.");
            }

            return states
                .TransferAsset(context, context.Signer, AgentAddress, 1 * Currencies.Mead)
                .SetState(
                    contractAddress,
                    List.Empty
                        .Add(context.Signer.Serialize())
                        .Add(false.Serialize())
                        .Add(RefillMead.Serialize())
                );
        }
    }
}
