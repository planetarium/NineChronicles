using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("activate_account2")]
    public class ActivateAccount : ActionBase, IActivateAccount
    {
        public Address PendingAddress { get; private set; }

        public byte[] Signature { get; private set; }

        Address IActivateAccount.PendingAddress => PendingAddress;
        byte[] IActivateAccount.Signature => Signature;

        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"pa", PendingAddress.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text)"s", (Binary) Signature),
                }
            );

        public ActivateAccount()
        {
        }

        public ActivateAccount(Address pendingAddress, byte[] signature)
        {
            PendingAddress = pendingAddress;
            Signature = signature;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta state = context.PreviousStates;
            Address activatedAddress = context.Signer.Derive(ActivationKey.DeriveKey);

            if (context.Rehearsal)
            {
                return state
                    .SetState(activatedAddress, MarkChanged)
                    .SetState(PendingAddress, MarkChanged);
            }

            if (!(state.GetState(activatedAddress) is null))
            {
                throw new AlreadyActivatedException($"{context.Signer} already activated.");
            }
            if (!state.TryGetState(PendingAddress, out Dictionary pendingAsDict))
            {
                throw new PendingActivationDoesNotExistsException(PendingAddress);
            }

            var pending = new PendingActivationState(pendingAsDict);

            if (pending.Verify(this))
            {
                // We left this log message to track activation history.
                // Please delete it if we have an API for evaluation results on the Libplanet side.
                Log.Information("{pendingAddress} is activated by {signer} now.", pending.address, context.Signer);
                return state
                    .SetState(activatedAddress, true.Serialize())
                    .SetState(pending.address, new Bencodex.Types.Null());
            }
            else
            {
                throw new InvalidSignatureException(pending, Signature);
            }
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;
            PendingAddress = asDict["pa"].ToAddress();
            Signature = (Binary) asDict["s"];
        }

        public Address GetPendingAddress() => PendingAddress;

        public byte[] GetSignature() => Signature;
    }
}
