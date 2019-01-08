using System;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action
{
    [ActionType("move_zone")]
    public class MoveZone : ActionBase
    {
        private readonly int _stage;

        public MoveZone(int stage)
        {
            _stage = stage;
        }

        public override void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            throw new NotImplementedException();
        }

        public override AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            throw new NotImplementedException();
        }

        public override IImmutableDictionary<string, object> PlainValue { get; }
    }
}
