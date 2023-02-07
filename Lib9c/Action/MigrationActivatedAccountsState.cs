using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    [ActionType("migration_activated_accounts_state")]
    public class MigrationActivatedAccountsState : GameAction, IMigrationActivatedAccountsStateV1
    {
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.SetState(Nekoyume.Addresses.ActivatedAccount, MarkChanged);
            }

            CheckPermission(context);

            Log.Debug($"Start {nameof(MigrationActivatedAccountsState)}");
            if (states.TryGetState(Nekoyume.Addresses.ActivatedAccount, out Dictionary rawState))
            {
                var activatedAccountsState = new ActivatedAccountsState(rawState);
                var accounts = activatedAccountsState.Accounts;
                foreach (var agentAddress in accounts)
                {
                    var address = agentAddress.Derive(ActivationKey.DeriveKey);
                    if (states.GetState(address) is null)
                    {
                        states = states.SetState(address, true.Serialize());
                    }
                }
                Log.Debug($"Finish {nameof(MigrationActivatedAccountsState)}");
                return states;
            }

            throw new ActivatedAccountsDoesNotExistsException();
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
        }
    }
}
