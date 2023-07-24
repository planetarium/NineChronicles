namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;
    using static Nekoyume.Model.State.StakeState;

    public class StakeStateTest
    {
        [Fact]
        public void IsClaimable()
        {
            Assert.False(new StakeState(
                    default,
                    0,
                    RewardInterval + 1,
                    LockupInterval,
                    new StakeAchievements())
                .IsClaimable(RewardInterval * 2));
            Assert.True(new StakeState(
                    default,
                    ActionObsoleteConfig.V100290ObsoleteIndex - 100,
                    ActionObsoleteConfig.V100290ObsoleteIndex - 100 + RewardInterval + 1,
                    ActionObsoleteConfig.V100290ObsoleteIndex - 100 + LockupInterval,
                    new StakeAchievements())
                .IsClaimable(ActionObsoleteConfig.V100290ObsoleteIndex - 100 + RewardInterval * 2));
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
        [InlineData(0L)]
        [InlineData(long.MaxValue)]
        [InlineData(long.MinValue)]
        public void Claim(long blockIndex)
        {
            var stakeState = new StakeState(new PrivateKey().ToAddress(), 0L);
            stakeState.Claim(blockIndex);
            Assert.Equal(blockIndex, stakeState.ReceivedBlockIndex);
        }

        [Theory]
        [InlineData(1L, ClaimStakeReward2.ObsoletedIndex - 1L, 0)]
        [InlineData(1L, ClaimStakeReward2.ObsoletedIndex, 0)]
        [InlineData(1L, ClaimStakeReward2.ObsoletedIndex + RewardInterval - 1L, 0)]
        [InlineData(1L, ClaimStakeReward2.ObsoletedIndex + RewardInterval, 1)]
        public void CalculateAccumulateRuneRewards(
            long startedBlockIndex,
            long blockIndex,
            int expected)
        {
            var state = new StakeState(default, startedBlockIndex);
            Assert.Equal(expected, state.CalculateAccumulatedRuneRewards(blockIndex));
        }

        [Theory]
        // default.
        [InlineData(0L, 0L, 0L, null, 0)]
        // control current block index.
        [InlineData(0L, 0L, RewardInterval, null, 1)]
        [InlineData(0L, 0L, RewardInterval * 99, null, 99)]
        // control started block index.
        [InlineData(1L, 0L, RewardInterval * 9, null, 8)]
        [InlineData(RewardInterval, 0L, RewardInterval * 9, null, 8)]
        [InlineData(RewardInterval + 1L, 0L, RewardInterval * 9, null, 7)]
        // control received block index.
        [InlineData(0L, 1L, RewardInterval * 9, null, 8)]
        [InlineData(0L, RewardInterval, RewardInterval * 9, null, 8)]
        [InlineData(0L, RewardInterval + 1L, RewardInterval * 9, null, 7)]
        // control reward start block index.
        [InlineData(0L, 0L, RewardInterval * 9, 0L, 9)]
        [InlineData(0L, 0L, RewardInterval * 9, 1L, 8)]
        [InlineData(0L, 0L, RewardInterval * 9, RewardInterval, 8)]
        [InlineData(0L, 0L, RewardInterval * 9, RewardInterval + 1L, 7)]
        // control complex. reward start block index is inside of reward range.
        [InlineData(1L, 0L, RewardInterval * 9, 1L, 8)]
        [InlineData(1L, 0L, RewardInterval * 9, RewardInterval + 1L, 7)]
        [InlineData(RewardInterval, 0L, RewardInterval * 9, RewardInterval, 8)]
        [InlineData(RewardInterval, 0L, RewardInterval * 9, RewardInterval + 1L, 7)]
        [InlineData(RewardInterval, RewardInterval * 2, RewardInterval * 9, RewardInterval, 7)]
        [InlineData(RewardInterval, RewardInterval * 2 + 1L, RewardInterval * 9, RewardInterval, 6)]
        // reward start block index is greater than reward range.
        [InlineData(0L, 0L, RewardInterval * 9, RewardInterval * 9 + 1L, 0)]
        // reward start block index is less than reward range.
        [InlineData(1L, 0L, RewardInterval * 9 + 1L, 0L, 9)]
        [InlineData(RewardInterval, RewardInterval * 2, RewardInterval * 9, 0L, 7)]
        public void GetRewardStepV1(
            long startedBlockIndex,
            long receivedBlockIndex,
            long currentBlockIndex,
            long? rewardStartBlockIndex,
            int expectedStep)
        {
            var stakeState = new StakeState(
                new PrivateKey().ToAddress(),
                startedBlockIndex);
            stakeState.Claim(receivedBlockIndex);
            var actualStep = stakeState.GetRewardStepV1(currentBlockIndex, rewardStartBlockIndex);
            Assert.Equal(expectedStep, actualStep);
        }

        [Theory]
        // default.
        [InlineData(0L, 0L, 0L, null, 0)]
        // control current block index.
        [InlineData(0L, 0L, RewardInterval, null, 1)]
        [InlineData(0L, 0L, RewardInterval * 99, null, 99)]
        // control started block index.
        [InlineData(1L, 0L, RewardInterval * 9, null, 8)]
        [InlineData(RewardInterval, 0L, RewardInterval * 9, null, 8)]
        [InlineData(RewardInterval + 1L, 0L, RewardInterval * 9, null, 7)]
        // control received block index.
        [InlineData(0L, 1L, RewardInterval * 9, null, 9)]
        [InlineData(0L, RewardInterval, RewardInterval * 9, null, 8)]
        [InlineData(0L, RewardInterval + 1L, RewardInterval * 9, null, 8)]
        // control reward start block index.
        [InlineData(0L, 0L, RewardInterval * 9, 0L, 9)]
        [InlineData(0L, 0L, RewardInterval * 9, 1L, 8)]
        [InlineData(0L, 0L, RewardInterval * 9, RewardInterval, 8)]
        [InlineData(0L, 0L, RewardInterval * 9, RewardInterval + 1L, 7)]
        // control complex. reward start block index is inside of reward range.
        [InlineData(1L, 0L, RewardInterval * 9, 1L, 8)]
        [InlineData(1L, 0L, RewardInterval * 9, RewardInterval + 1L, 7)]
        [InlineData(RewardInterval, 0L, RewardInterval * 9, RewardInterval, 8)]
        [InlineData(RewardInterval, 0L, RewardInterval * 9, RewardInterval + 1L, 7)]
        [InlineData(RewardInterval, RewardInterval * 2, RewardInterval * 9, RewardInterval, 7)]
        [InlineData(RewardInterval, RewardInterval * 2 + 1L, RewardInterval * 9, RewardInterval, 7)]
        // reward start block index is greater than reward range.
        [InlineData(0L, 0L, RewardInterval * 9, RewardInterval * 9 + 1L, 0)]
        // reward start block index is less than reward range.
        [InlineData(1L, 0L, RewardInterval * 9 + 1L, 0L, 9)]
        [InlineData(RewardInterval, RewardInterval * 2, RewardInterval * 9, 0L, 7)]
        public void GetRewardStep(
            long startedBlockIndex,
            long receivedBlockIndex,
            long currentBlockIndex,
            long? rewardStartBlockIndex,
            int expectedStep)
        {
            var stakeState = new StakeState(
                new PrivateKey().ToAddress(),
                startedBlockIndex);
            stakeState.Claim(receivedBlockIndex);
            var actualStep = stakeState.GetRewardStep(currentBlockIndex, rewardStartBlockIndex);
            Assert.Equal(expectedStep, actualStep);
        }
    }
}
