namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Nekoyume.Model.State;
    using Xunit;

    public class StakeStateTest
    {
        [Fact]
        public void Serialize()
        {
            var state = new StakeState(default, 100);

            var serialized = (Dictionary)state.Serialize();
            var deserialized = new StakeState(serialized);

            Assert.Equal(state.address, deserialized.address);
            Assert.Equal(state.StartedBlockIndex, deserialized.StartedBlockIndex);
            Assert.Equal(state.ReceivedBlockIndex, deserialized.ReceivedBlockIndex);
            Assert.Equal(state.CancellableBlockIndex, deserialized.CancellableBlockIndex);
        }

        [Fact]
        public void SerializeV2()
        {
            var state = new StakeState(default, 100);

            var serialized = (Dictionary)state.SerializeV2();
            var deserialized = new StakeState(serialized);

            Assert.Equal(state.address, deserialized.address);
            Assert.Equal(state.StartedBlockIndex, deserialized.StartedBlockIndex);
            Assert.Equal(state.ReceivedBlockIndex, deserialized.ReceivedBlockIndex);
            Assert.Equal(state.CancellableBlockIndex, deserialized.CancellableBlockIndex);
        }
    }
}
