namespace Lib9c.Tests.Action
{
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;

    public class State : IAccountStateDelta
    {
        private readonly ImmutableDictionary<Address, IValue> _state;

        public State()
            : this(ImmutableDictionary<Address, IValue>.Empty)
        {
        }

        public State(ImmutableDictionary<Address, IValue> state)
        {
            _state = state;
        }

        public IImmutableSet<Address> UpdatedAddresses =>
            _state.Keys.ToImmutableHashSet();

        public IValue GetState(Address address) =>
            _state.TryGetValue(address, out IValue value) ? value : null;

        public IAccountStateDelta SetState(Address address, IValue state)
        {
            return new State(_state.SetItem(address, state));
        }
    }
}
