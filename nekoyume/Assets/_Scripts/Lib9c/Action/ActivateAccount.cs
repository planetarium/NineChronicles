using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("activate_account")]
    public class ActivateAccount : ActionBase
    {
        public Address PendingAddress { get; private set; }

        public byte[] Signature { get; private set; }

        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"pending_address", PendingAddress.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text)"signature", (Binary) Signature),
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

            if (context.Rehearsal)
            {
                return state
                    .SetState(ActivatedAccountsState.Address, MarkChanged)
                    .SetState(PendingAddress, MarkChanged);
            }

            if (!state.TryGetState(ActivatedAccountsState.Address, out Dictionary accountsAsDict))
            {
                throw new ActivatedAccountsDoesNotExistsException();
            }
            if (!state.TryGetState(PendingAddress, out Dictionary pendingAsDict))
            {
                throw new PendingActivationDoesNotExistsException(PendingAddress);
            }

            var accounts = new ActivatedAccountsState(accountsAsDict);
            var pending = new PendingActivationState(pendingAsDict);

            if (pending.PublicKey.Verify(pending.Nonce, Signature))
            {
                return state.SetState(
                    ActivatedAccountsState.Address,
                    accounts.AddAccount(context.Signer).Serialize()
                ).SetState(
                    pending.address,
                    new Bencodex.Types.Null()
                );
            }
            else
            {
                throw new InvalidSignatureException(pending, Signature);
            }
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;
            PendingAddress = asDict["pending_address"].ToAddress();
            Signature = (Binary) asDict["signature"];
        }
    }
}
