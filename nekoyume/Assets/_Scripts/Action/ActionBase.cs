using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Move;

namespace Nekoyume.Action
{
    public abstract class ActionBase : IAction
    {
        public abstract Context Execute(Context ctx);

        public abstract Dictionary<string, string> ToDetails();
        public void LoadPlainValue(IImmutableDictionary<string, object> plainValue)
        {
            throw new System.NotImplementedException();
        }

        public ISet<Address> RequestStates(Address @from, Address to)
        {
            return new HashSet<Address> {from, to}.ToImmutableHashSet();
        }

        public AddressStateMap Execute(Address @from, Address to, AddressStateMap states)
        {
            throw new System.NotImplementedException();
        }

        public IImmutableDictionary<string, object> PlainValue { get; }
    }
}
