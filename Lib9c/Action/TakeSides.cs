using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("take_sides")]
    public class TakeSides : ActionBase
    {
        public TakeSides()
        {
        }

        public Address ValkyrieAddress;
        public override IValue PlainValue => ValkyrieAddress.Serialize();
        public override void LoadPlainValue(IValue plainValue)
        {
            ValkyrieAddress = plainValue.ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            Address signer = context.Signer;
            var states = context.PreviousStates.TransferAsset(ValkyrieAddress, signer, 1 * Currencies.Mead);
            var contractAddress = signer.Derive(nameof(BringEinheri));
            if (!states.TryGetState(contractAddress, out List contract))
            {
                throw new InvalidAddressException();
            }

            if (contract[0].ToAddress() != ValkyrieAddress)
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
                    .Add(ValkyrieAddress.Serialize())
                    .Add(true.Serialize())
            );
        }
    }
}
