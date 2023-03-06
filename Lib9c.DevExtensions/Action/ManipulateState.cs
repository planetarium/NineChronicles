using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("manipulate_state")]
    public class ManipulateState : GameAction
    {
        public List<(Address, IValue)> StateList { get; set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var state = context.PreviousStates;
            foreach (var (address, value) in StateList)
            {
                state = state.SetState(address, value);
            }

            return state;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["stateList"] = StateList.Serialize()
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            StateList = plainValue["stateList"].ToStateList();
        }
    }
}
