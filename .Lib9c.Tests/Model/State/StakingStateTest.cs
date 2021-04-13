namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Model.State;
    using Xunit;

    public class StakingStateTest
    {
        private readonly Address _address;

        public StakingStateTest()
        {
            _address = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
        }

        [Fact]
        public void Serialize()
        {
            StakingState stakingState = new StakingState(_address, 1, 10000);
            Dictionary serialized = (Dictionary)stakingState.Serialize();
            Assert.Equal(stakingState, new StakingState(serialized));
        }

        [Fact]
        public void Serialize_DotNet_API()
        {
            StakingState stakingState = new StakingState(_address, 1, 10000);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, stakingState);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (StakingState)formatter.Deserialize(ms);

            Assert.Equal(stakingState, deserialized);
        }

        [Fact]
        public void Update()
        {
            StakingState stakingState = new StakingState(_address, 1, 10000);
            Assert.Equal(1, stakingState.Level);
            Assert.Equal(10000, stakingState.StartedBlockIndex);
            Assert.Equal(26000, stakingState.ExpiredBlockIndex);

            stakingState.Update(2);
            Assert.Equal(2, stakingState.Level);
        }
    }
}
