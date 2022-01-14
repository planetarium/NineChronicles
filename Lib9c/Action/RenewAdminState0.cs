using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("renew_admin_state")]
    public class RenewAdminState0 : GameAction
    {
        private const string NewValidUntilKey = "new_valid_until";
        public long NewValidUntil {get; internal set; }

        public RenewAdminState0()
        {
        }

        public RenewAdminState0(long newValidUntil)
        {
            NewValidUntil = newValidUntil;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states
                    .SetState(Addresses.Admin, MarkChanged);
            }

            if (TryGetAdminState(context, out AdminState adminState))
            {
                if (context.Signer != adminState.AdminAddress)
                {
                    throw new PermissionDeniedException(adminState, context.Signer);
                }

                var newAdminState = new AdminState(adminState.AdminAddress, NewValidUntil);
                states = states.SetState(Addresses.Admin,
                    newAdminState.Serialize());
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [NewValidUntilKey] = (Integer)NewValidUntil,
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            NewValidUntil = plainValue[NewValidUntilKey].ToLong();
        }
    }
}
