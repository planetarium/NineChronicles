using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("migration_activated_accounts_state")]
    public class MigrationActivatedAccountsState : GameAction
    {
        public List<Address> Addresses;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                states = Addresses
                    .Aggregate(states, (current, address) =>
                        current.SetState(address.Derive(ActivationKey.DeriveKey), MarkChanged));

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
                        if (activatedAccountsState.Accounts.Contains(agentAddress))
                        {
                            activatedAccountsState.Remove(agentAddress);
                        }
                    }
                    else
                    {
                        // Prevent override state.
                        throw new InvalidAddressException($"Address({address}) duplicated.");
                    }
                }

                Log.Debug($"Finish {nameof(MigrationActivatedAccountsState)}: {activatedAccountsState.Accounts.Count}");
                return states.SetState(Nekoyume.Addresses.ActivatedAccount, activatedAccountsState.Serialize());
            }

            throw new ActivatedAccountsDoesNotExistsException();
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["a"] = new List(Addresses.Select(a => a.Serialize()))
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Addresses = plainValue["a"].ToList(a => a.ToAddress());
        }
    }
}
