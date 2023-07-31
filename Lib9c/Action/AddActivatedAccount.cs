using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("add_activated_account2")]
    [ActionObsolete(ActionObsoleteConfig.V200030ObsoleteIndex)]
    public class AddActivatedAccount : ActionBase, IAddActivatedAccountV1
    {
        public AddActivatedAccount(Address address)
        {
            Address = address;
        }

        public AddActivatedAccount()
        {
        }

        public Address Address { get; private set; }

        Address IAddActivatedAccountV1.Address => Address;

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "add_activated_account2")
            .Add("values", new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"a", Address.Serialize()),
                }
            ));

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            IAccountStateDelta state = context.PreviousState;
            var address = Address.Derive(ActivationKey.DeriveKey);

            if (context.Rehearsal)
            {
                return state
                    .SetState(address, MarkChanged);
            }

            CheckObsolete(ActionObsoleteConfig.V200030ObsoleteIndex, context);
            if (!(state.GetState(address) is null))
            {
                throw new AlreadyActivatedException($"{address} is already activated.");
            }

            CheckPermission(context);

            return state.SetState(address, true.Serialize());
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)((Dictionary)plainValue)["values"];
            Address = asDict["a"].ToAddress();
        }
    }
}
