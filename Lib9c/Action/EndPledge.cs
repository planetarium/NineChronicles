using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Libplanet.State;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("end_pledge")]
    public class EndPledge : ActionBase
    {
        public EndPledge()
        {
        }

        public Address AgentAddress;
        public override IValue PlainValue => AgentAddress.Serialize();
        public override void LoadPlainValue(IValue plainValue)
        {
            AgentAddress = plainValue.ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            Address signer = context.Signer;
            var states = context.PreviousStates;
            var contractAddress = AgentAddress.GetPledgeAddress();
            if (states.TryGetState(contractAddress, out List contract))
            {
                if (signer != contract[0].ToAddress())
                {
                    throw new InvalidAddressException($"{signer} is not patron.");
                }

                var balance = states.GetBalance(AgentAddress, Currencies.Mead);
                if (balance > 0 * Currencies.Mead)
                {
                    states = states.TransferAsset(AgentAddress, signer, balance);
                }
                return states.SetState(contractAddress, Null.Value);
            }

            throw new FailedLoadStateException("failed to find pledge.");
        }
    }
}
