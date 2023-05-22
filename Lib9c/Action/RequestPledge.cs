using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("request_pledge")]
    public class RequestPledge : ActionBase
    {
        public RequestPledge()
        {
        }

        // Value from tx per block policy.
        // https://github.com/planetarium/lib9c/blob/b6c1e85abc0b93347dae8e1a12aaefd767b27632/Lib9c.Policy/Policy/MaxTransactionsPerSignerPerBlockPolicy.cs#L29
        public const int RefillMead = 4;
        public Address AgentAddress;

        public override IValue PlainValue => AgentAddress.Serialize();

        public override void LoadPlainValue(IValue plainValue)
        {
            AgentAddress = plainValue.ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousStates;
            var contractAddress = AgentAddress.GetPledgeAddress();
            if (states.TryGetState(contractAddress, out List _))
            {
                throw new AlreadyReceivedException("");
            }

            return states
                .TransferAsset(context.Signer, AgentAddress, 1 * Currencies.Mead)
                .SetState(
                    contractAddress,
                    List.Empty
                        .Add(context.Signer.Serialize())
                        .Add(false.Serialize())
                );
        }
    }
}
