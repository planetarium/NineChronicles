using System;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action
{
    [ActionType("hack_and_slash")]
    public class HackAndSlash : ActionBase
    {
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
