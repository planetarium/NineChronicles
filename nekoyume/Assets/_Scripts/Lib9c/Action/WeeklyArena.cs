using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("weekly_arena")]
    public class WeeklyArena : ActionBase
    {
        public Address ArenaAddress;

        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "arenaAddress"] = ArenaAddress.Serialize(),
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary) plainValue;
            ArenaAddress = dict["arenaAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ArenaAddress, MarkChanged);
            }

            if (!(states.GetState(ArenaAddress) is null))
            {
                return states;
            }

            var arenaState = new WeeklyArenaState(ArenaAddress);
            return states.SetState(ArenaAddress, arenaState.Serialize());
        }
    }
}
