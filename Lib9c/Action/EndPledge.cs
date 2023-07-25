using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("end_pledge")]
    public class EndPledge : ActionBase
    {
        public const string TypeIdentifier = "end_pledge";
        public EndPledge()
        {
        }

        public Address AgentAddress;
        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", AgentAddress.Serialize());
        public override void LoadPlainValue(IValue plainValue)
        {
            AgentAddress = ((Dictionary)plainValue)["values"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            Address signer = context.Signer;
            var states = context.PreviousState;
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
                    states = states.TransferAsset(context, AgentAddress, signer, balance);
                }
                return states.SetState(contractAddress, Null.Value);
            }

            throw new FailedLoadStateException("failed to find pledge.");
        }
    }
}
