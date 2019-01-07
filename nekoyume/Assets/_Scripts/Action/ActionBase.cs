using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action
{
    public abstract class ActionBase : IAction
    {
        public abstract Dictionary<string, string> ToDetails();
        public virtual void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            throw new System.NotImplementedException();
        }

        public virtual ISet<Address> RequestStates(Address @from, Address to)
        {
            return new HashSet<Address> {from, to}.ToImmutableHashSet();
        }

        public virtual AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            throw new System.NotImplementedException();
        }

        public virtual IImmutableDictionary<string, object> PlainValue { get; }
    }
}
