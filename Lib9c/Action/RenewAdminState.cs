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
    public class RenewAdminState : GameAction
    {
        private const string NewValidUntilKey = "new_valid_until";
        public long NewValidUntil {get; internal set; }

        public RenewAdminState()
        {
        }

        public RenewAdminState(long newValidUntil)
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

            CheckPermission(context);

            if (TryGetAdminState(context, out AdminState adminState))
            {
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
