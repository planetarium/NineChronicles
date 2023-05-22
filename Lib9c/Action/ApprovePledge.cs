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
            var contractAddress = signer.Derive(nameof(RequestPledge));
            if (!states.TryGetState(contractAddress, out List contract))
            {
                throw new InvalidAddressException();
            }

            if (contract[0].ToAddress() != PatronAddress)
            {
                throw new InvalidAddressException();
            }

            if (contract[1].ToBoolean())
            {
                throw new AlreadyActivatedException("");
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
