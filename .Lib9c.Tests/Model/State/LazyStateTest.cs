namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Tests.TestHelper;
    using Libplanet;
    using Nekoyume.Model.State;
    using Xunit;
    using LazySampleState = Nekoyume.Model.State.LazyState<
        Lib9c.Tests.Model.State.LazyStateTest.SampleState,
        Bencodex.Types.Dictionary
    >;

    public class LazyStateTest
    {
        private readonly Address _address;
        private readonly SampleState _state;
        private readonly Dictionary _serializedEncoding;
        private readonly LazyState<SampleState, Dictionary> _loaded;
        private readonly LazyState<SampleState, Dictionary> _unloaded;

        public LazyStateTest()
        {
            _address = new Address("66eD03107F270d082AC1F71d8E50375f9372d8fC");
            _state = new SampleState(_address, 123L, "hello");
            _serializedEncoding = (Dictionary)_state.Serialize();
            _loaded = new LazySampleState(_state);
            _unloaded = new LazySampleState(_serializedEncoding, d => new SampleState(d));
        }

        [Fact]
        public void State()
        {
            Assert.Same(_state, _loaded.State);
            Assert.True(_loaded.GetStateOrSerializedEncoding(out _, out _));
            Assert.False(_unloaded.GetStateOrSerializedEncoding(out _, out _));

            var state = _unloaded.State;
            Assert.Equal(123L, state.Foo);
            Assert.Equal("hello", state.Bar);
            Assert.True(_unloaded.GetStateOrSerializedEncoding(out _, out _));

            Assert.Same(state, _unloaded.State);
        }

        [Fact]
        public void Serialize()
        {
            _loaded.Serialize().ShouldBe(_serializedEncoding);
            Assert.True(_loaded.GetStateOrSerializedEncoding(out _, out _));

            Assert.Same(_serializedEncoding, _unloaded.Serialize());
            Assert.False(_unloaded.GetStateOrSerializedEncoding(out _, out _));

            _unloaded.State.Foo = 456L;
            Assert.Equal(
                456L,
                (long)((Dictionary)_unloaded.Serialize()).GetValue<Integer>("foo")
            );
            Assert.True(_unloaded.GetStateOrSerializedEncoding(out _, out _));
        }

        [Fact]
        public void LoadState()
        {
            Assert.Same(_state, LazySampleState.LoadState(_loaded));
            Assert.True(_loaded.GetStateOrSerializedEncoding(out _, out _));
            Assert.False(_unloaded.GetStateOrSerializedEncoding(out _, out _));

            var state = LazySampleState.LoadState(_unloaded);
            Assert.Equal(123L, state.Foo);
            Assert.Equal("hello", state.Bar);
            Assert.True(_unloaded.GetStateOrSerializedEncoding(out _, out _));

            Assert.Same(state, LazySampleState.LoadState(_unloaded));
        }

        [Fact]
        public void LoadStatePair()
        {
            var loadedPair = LazySampleState.LoadStatePair(KeyValuePair.Create('k', _loaded));
            Assert.Equal('k', loadedPair.Key);
            Assert.Same(_state, loadedPair.Value);
            Assert.True(_loaded.GetStateOrSerializedEncoding(out _, out _));
            Assert.False(_unloaded.GetStateOrSerializedEncoding(out _, out _));

            var unloadedPair = LazySampleState.LoadStatePair(KeyValuePair.Create('K', _unloaded));
            Assert.Equal('K', unloadedPair.Key);
            Assert.Equal(123L, unloadedPair.Value.Foo);
            Assert.Equal("hello", unloadedPair.Value.Bar);
            Assert.True(_unloaded.GetStateOrSerializedEncoding(out _, out _));

            var unloadedPair2 = LazySampleState.LoadStatePair(KeyValuePair.Create(2, _unloaded));
            Assert.Equal(2, unloadedPair2.Key);
            Assert.Same(unloadedPair.Value, unloadedPair2.Value);
        }

        public class SampleState : State
        {
            public SampleState(Address address, long foo, string bar)
                : base(address)
            {
                Foo = foo;
                Bar = bar;
            }

            public SampleState(Dictionary serialized)
                : base(serialized)
            {
                Foo = serialized.GetValue<Integer>("foo");
                Bar = serialized.GetValue<Text>("bar");
            }

            public SampleState(IValue iValue)
                : this((Dictionary)iValue)
            {
            }

            public long Foo { get; set; }

            public string Bar { get; set; }

            public override IValue Serialize() => ((Dictionary)base.Serialize())
                .Add("foo", Foo)
                .Add("bar", Bar);
        }
    }
}
