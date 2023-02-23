namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Model.State;
    using Xunit;

    public class StakeStateTest
    {
        [Fact]
        public void IsClaimable()
        {
            Assert.False(new StakeState(
                    default,
                    0,
                    StakeState.RewardInterval + 1,
                    StakeState.LockupInterval,
                    new StakeState.StakeAchievements())
                .IsClaimable(StakeState.RewardInterval * 2));
            Assert.True(new StakeState(
                    default,
                    ActionObsoleteConfig.V100290ObsoleteIndex - 100,
                    ActionObsoleteConfig.V100290ObsoleteIndex - 100 + StakeState.RewardInterval + 1,
                    ActionObsoleteConfig.V100290ObsoleteIndex - 100 + StakeState.LockupInterval,
                    new StakeState.StakeAchievements())
                .IsClaimable(ActionObsoleteConfig.V100290ObsoleteIndex - 100 + StakeState.RewardInterval * 2));
        }

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

        [Theory]
        [InlineData(1L, 1L, -110)]
        [InlineData(1L, ClaimStakeReward.ObsoletedIndex, 0)]
        [InlineData(1L, ClaimStakeReward.ObsoletedIndex + StakeState.RewardInterval, 1)]
        public void CalculateAccumulateRuneRewards(
            long startedBlockIndex,
            long blockIndex,
            int expected)
        {
            var state = new StakeState(default, startedBlockIndex);
            Assert.Equal(expected, state.CalculateAccumulatedRuneRewards(blockIndex));
        }
    }
}
