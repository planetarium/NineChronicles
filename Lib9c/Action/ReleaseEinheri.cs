using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("release_einheri")]
    public class ReleaseEinheri : ActionBase
    {
        public ReleaseEinheri()
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
            Address signer = context.Signer;
            var states = context.PreviousStates.Mead(signer, 1);
            var contractAddress = EinheriAddress.Derive(nameof(BringEinheri));
            if (states.TryGetState(contractAddress, out List contract))
            {
                if (signer != contract[0].ToAddress())
                {
                    throw new InvalidAddressException();
                }

                var balance = states.GetBalance(EinheriAddress, Currencies.Mead);
                if (balance > 0 * Currencies.Mead)
                {
                    states = states.TransferAsset(EinheriAddress, signer, balance);
                }
                return states.SetState(contractAddress, Null.Value);
            }

            throw new FailedLoadStateException("");
        }
    }
}
