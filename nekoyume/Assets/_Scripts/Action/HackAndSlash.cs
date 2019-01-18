using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [ActionType("hack_and_slash")]
    public class HackAndSlash : ActionBase
    {
        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
        }

        public override AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            var ctx = (Context) states.GetValueOrDefault(to);
            if (ctx.avatar.Dead)
            {
                throw new InvalidActionException();
            }

            var simulator = new Simulator(0, ctx.avatar);
            var player = simulator.Simulate();
            ctx.avatar.Update(player);
            return (AddressStateMap) states.SetItem(to, ctx);
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
        }.ToImmutableDictionary();
    }
}
