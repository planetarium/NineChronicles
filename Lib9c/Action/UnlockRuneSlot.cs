using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("unlock_rune_slot")]
    public class UnlockRuneSlot : GameAction
    {
        protected override IImmutableDictionary<string, IValue> PlainValueInternal { get; }
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            throw new NotImplementedException();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            throw new NotImplementedException();
        }

    }
}
