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

        // Value from tx per block policy.
        // https://github.com/planetarium/lib9c/blob/b6c1e85abc0b93347dae8e1a12aaefd767b27632/Lib9c.Policy/Policy/MaxTransactionsPerSignerPerBlockPolicy.cs#L29
        public const int RefillMead = 4;
        public Address EinheriAddress;

        public override IValue PlainValue => EinheriAddress.Serialize();

        public override void LoadPlainValue(IValue plainValue)
        {
            EinheriAddress = plainValue.ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
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
