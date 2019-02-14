using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action
{
    public abstract class ActionBase : IAction
    {
        public abstract void LoadPlainValue(IImmutableDictionary<string, object> plainValue);

        public IImmutableSet<Address> RequestStates(Address @from, Address to)
        {
            return new HashSet<Address> {from, to}.ToImmutableHashSet();
        }

        public abstract AddressStateMap Execute(IActionContext ctx);

        public abstract IImmutableDictionary<string, object> PlainValue { get; }
    }
}
