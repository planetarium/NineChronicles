using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;

namespace Lib9c.Tests.Action
{
    public class State : IAccountStateDelta
    {
        private readonly ImmutableDictionary<Address, IValue> _state;
        public IImmutableSet<Address> UpdatedAddresses =>
            _state.Keys.ToImmutableHashSet();

        public State(ImmutableDictionary<Address, IValue> state)
        {
            _state = state;
        }

        public IValue GetState(Address address)
        {
            if (_state.TryGetValue(address, out IValue value))
            {
                return value;
            }

            return null;
        }

        public IAccountStateDelta SetState(Address address, IValue state)
        {
            return new State(_state.SetItem(address, state));
        }
    }
}
