using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("approve_pledge")]
    public class ApprovePledge : ActionBase
    {
        public ApprovePledge()
        {
        }

        public Address PatronAddress;
        public override IValue PlainValue => PatronAddress.Serialize();
        public override void LoadPlainValue(IValue plainValue)
        {
            PatronAddress = plainValue.ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            Address signer = context.Signer;
            var states = context.PreviousStates;
            var contractAddress = signer.GetPledgeAddress();
            if (!states.TryGetState(contractAddress, out List contract))
            {
                throw new FailedLoadStateException("failed to find requested pledge.");
            }

            if (contract[0].ToAddress() != PatronAddress)
            {
                throw new InvalidAddressException("invalid patron address.");
            }

            if (contract[1].ToBoolean())
            {
                throw new AlreadyContractedException($"{signer} already contracted.");
            }

            return states.SetState(
                contractAddress,
                List.Empty
                    .Add(PatronAddress.Serialize())
                    .Add(true.Serialize())
            );
        }
    }
}
