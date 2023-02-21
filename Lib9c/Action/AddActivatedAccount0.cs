using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("add_activated_account")]
    public class AddActivatedAccount0 : ActionBase, IAddActivatedAccountV1
    {
        public AddActivatedAccount0(Address address)
        {
            Address = address;
        }

        public AddActivatedAccount0()
        {
        }

        public Address Address { get; private set; }

        Address IAddActivatedAccountV1.Address => Address;

        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"address", Address.Serialize()),
                }
            );

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta state = context.PreviousStates;

            if (context.Rehearsal)
            {
                return state
                    .SetState(ActivatedAccountsState.Address, MarkChanged);
            }

            if (!state.TryGetState(ActivatedAccountsState.Address, out Dictionary accountsAsDict))
            {
                throw new ActivatedAccountsDoesNotExistsException();
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);
            CheckPermission(context);

            var accounts = new ActivatedAccountsState(accountsAsDict);
            return state.SetState(
                ActivatedAccountsState.Address,
                accounts.AddAccount(Address).Serialize()
            );
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;
            Address = asDict["address"].ToAddress();
        }
    }
}
