using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
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
            var avatar = (Avatar) states.GetValueOrDefault(to);
            if (avatar.Dead)
            {
                throw new InvalidActionException();
            }

            var simulator = new Simulator(0, avatar);
            var player = simulator.Simulate();
            avatar.Update(player);
            return (AddressStateMap) states.SetItem(to, avatar);
        }

        public override IImmutableDictionary<string, object> PlainValue => new Dictionary<string, object>
        {
        }.ToImmutableDictionary();
    }
}
