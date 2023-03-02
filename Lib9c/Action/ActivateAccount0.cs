using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("activate_account")]
    public class ActivateAccount0 : ActionBase, IActivateAccount
    {
        public Address PendingAddress { get; private set; }

        public byte[] Signature { get; private set; }

        Address IActivateAccount.PendingAddress => PendingAddress;
        byte[] IActivateAccount.Signature => Signature;

        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"pending_address", PendingAddress.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text)"signature", (Binary) Signature),
                }
            );

        public ActivateAccount0()
        {
        }

        public ActivateAccount0(Address pendingAddress, byte[] signature)
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

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

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

            if (pending.Verify(Signature))
            {
                // We left this log message to track activation history.
                // Please delete it if we have an API for evaluation results on the Libplanet side.
                Log.Information("{pendingAddress} is activated by {signer} now.", pending.address, context.Signer);
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

        public Address GetPendingAddress() => PendingAddress;

        public byte[] GetSignature() => Signature;
    }
}
