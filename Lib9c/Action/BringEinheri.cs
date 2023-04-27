using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("bring_einheri")]
    public class BringEinheri : ActionBase
    {
        public BringEinheri()
        {
        }

        public Address EinheriAddress;

        public override IValue PlainValue => EinheriAddress.Serialize();

        public override void LoadPlainValue(IValue plainValue)
        {
            EinheriAddress = plainValue.ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var signer = context.Signer;
            var states = context.PreviousStates.Mead(signer, 1);
            var contractAddress = EinheriAddress.Derive(nameof(BringEinheri));
            if (states.TryGetState(contractAddress, out List _))
            {
                throw new AlreadyReceivedException("");
            }

            return states
                .TransferAsset(context.Signer, EinheriAddress, 1 * Currencies.Mead)
                .SetState(
                    contractAddress,
                    List.Empty
                        .Add(context.Signer.Serialize())
                        .Add(false.Serialize())
                );
        }
    }
}
