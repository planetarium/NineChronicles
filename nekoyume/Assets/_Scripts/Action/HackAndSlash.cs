using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action
{
    [ActionType("hack_and_slash")]
    public class HackAndSlash : ActionBase
    {
        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>().ToImmutableDictionary();

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
        }

        public override AddressStateMap Execute(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            var to = actionCtx.To;
            var ctx = (Context) states.GetValueOrDefault(to);
            if (ctx.avatar.Dead)
            {
                throw new InvalidActionException();
            }

            var simulator = new Simulator(0, ctx.avatar);
            var player = simulator.Simulate();
            ctx.avatar.Update(player);
            ctx.battleLog = simulator.log;
            return (AddressStateMap) states.SetItem(to, ctx);
        }
    }
}
