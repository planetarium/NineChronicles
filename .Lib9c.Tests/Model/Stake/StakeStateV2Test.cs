namespace Lib9c.Tests.Model.Stake
{
    using System;
    using Libplanet.Crypto;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Xunit;

    public class StakeStateV2Test
    {
        [Fact]
        public void DeriveAddress()
        {
            var agentAddr = new PrivateKey().ToAddress();
            var expectedStakeStateAddr = StakeState.DeriveAddress(agentAddr);
            Assert.Equal(expectedStakeStateAddr, StakeStateV2.DeriveAddress(agentAddr));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void Constructor_Default(long startedBlockIndex, long receivedBlockIndex)
        {
            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            var state = new StakeStateV2(contract, startedBlockIndex, receivedBlockIndex);
            Assert.Equal(contract, state.Contract);
            Assert.Equal(startedBlockIndex, state.StartedBlockIndex);
            Assert.Equal(receivedBlockIndex, state.ReceivedBlockIndex);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void Constructor_Throw_ArgumentNullException(
            long startedBlockIndex,
            long receivedBlockIndex)
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StakeStateV2(null, startedBlockIndex, receivedBlockIndex));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(-1, -1)]
        public void Constructor_Throw_ArgumentOutOfRangeException(
            long startedBlockIndex,
            long receivedBlockIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StakeStateV2(null, startedBlockIndex, receivedBlockIndex));
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(0, 0)]
        [InlineData(long.MaxValue, null)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void Constructor_StakeState(long startedBlockIndex, long? receivedBlockIndex)
        {
            var stakeState = new StakeState(
                new PrivateKey().ToAddress(),
                startedBlockIndex);
            if (receivedBlockIndex.HasValue)
            {
                stakeState.Claim(receivedBlockIndex.Value);
            }

            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            var stakeStateV2 = new StakeStateV2(stakeState, contract);
            Assert.Equal(contract, stakeStateV2.Contract);
            Assert.Equal(stakeState.StartedBlockIndex, stakeStateV2.StartedBlockIndex);
            Assert.Equal(stakeState.ReceivedBlockIndex, stakeStateV2.ReceivedBlockIndex);
        }

        [Fact]
        public void Constructor_StakeState_Throw_ArgumentNullException()
        {
            var stakeState = new StakeState(new PrivateKey().ToAddress(), 0);
            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            Assert.Throws<ArgumentNullException>(() => new StakeStateV2(null, contract));
            Assert.Throws<ArgumentNullException>(() => new StakeStateV2(stakeState, null));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void Serde(long startedBlockIndex, long receivedBlockIndex)
        {
            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            var state = new StakeStateV2(contract, startedBlockIndex, receivedBlockIndex);
            var ser = state.Serialize();
            var des = new StakeStateV2(ser);
            Assert.Equal(state.Contract, des.Contract);
            Assert.Equal(state.StartedBlockIndex, des.StartedBlockIndex);
            Assert.Equal(state.ReceivedBlockIndex, des.ReceivedBlockIndex);
            var ser2 = des.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Fact]
        public void Compare()
        {
            var contract = new Contract(
                Contract.StakeRegularFixedRewardSheetPrefix,
                Contract.StakeRegularRewardSheetPrefix,
                1,
                1);
            var stateL = new StakeStateV2(contract, 0);
            var stateR = new StakeStateV2(contract, 0);
            Assert.Equal(stateL, stateR);
            stateR = new StakeStateV2(contract, 1);
            Assert.NotEqual(stateL, stateR);
        }
    }
}
