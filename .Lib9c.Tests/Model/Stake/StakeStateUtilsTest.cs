namespace Lib9c.Tests.Model.Stake
{
    using System;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Fixtures.TableCSV;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.TableData.Stake;
    using Xunit;

    public class StakeStateUtilsTest
    {
        [Fact]
        public void TryMigrate_Throw_NullReferenceException_When_IAccountDelta_Null()
        {
            Assert.Throws<NullReferenceException>(() =>
                StakeStateUtils.TryMigrate(null, default, out _));
        }

        [Fact]
        public void TryMigrate_Return_False_When_Staking_State_Null()
        {
            var state = new MockStateDelta();
            Assert.False(StakeStateUtils.TryMigrate(state, new PrivateKey().ToAddress(), out _));
        }

        [Theory]
        [InlineData(
            0,
            null,
            "StakeRegularFixedRewardSheet_V1",
            "StakeRegularRewardSheet_V1")]
        [InlineData(
            0,
            long.MaxValue,
            "StakeRegularFixedRewardSheet_V1",
            "StakeRegularRewardSheet_V1")]
        // NOTE: "stake_regular_reward_sheet_v2_start_block_index" in
        //       GameConfigSheetFixtures.Default is 5,510,416.
        [InlineData(
            5_510_416 - 1,
            null,
            "StakeRegularFixedRewardSheet_V1",
            "StakeRegularRewardSheet_V1")]
        [InlineData(
            5_510_416,
            null,
            "StakeRegularFixedRewardSheet_V1",
            "StakeRegularRewardSheet_V2")]
        // NOTE: "stake_regular_fixed_reward_sheet_v2_start_block_index" and
        //       "stake_regular_reward_sheet_v3_start_block_index" in
        //       GameConfigSheetFixtures.Default is 6,700,000.
        [InlineData(
            6_700_000 - 1,
            null,
            "StakeRegularFixedRewardSheet_V1",
            "StakeRegularRewardSheet_V2")]
        [InlineData(
            6_700_000,
            null,
            "StakeRegularFixedRewardSheet_V2",
            "StakeRegularRewardSheet_V3")]
        // NOTE: "stake_regular_reward_sheet_v4_start_block_index" in
        //       GameConfigSheetFixtures.Default is 6,910,000.
        [InlineData(
            6_910_000 - 1,
            null,
            "StakeRegularFixedRewardSheet_V2",
            "StakeRegularRewardSheet_V3")]
        [InlineData(
            6_910_000,
            null,
            "StakeRegularFixedRewardSheet_V2",
            "StakeRegularRewardSheet_V4")]
        // NOTE: "stake_regular_reward_sheet_v5_start_block_index" in
        //       GameConfigSheetFixtures.Default is 7,650,000.
        [InlineData(
            7_650_000 - 1,
            null,
            "StakeRegularFixedRewardSheet_V2",
            "StakeRegularRewardSheet_V4")]
        [InlineData(
            7_650_000,
            null,
            "StakeRegularFixedRewardSheet_V2",
            "StakeRegularRewardSheet_V5")]
        // NOTE: latest.
        [InlineData(
            long.MaxValue,
            null,
            "StakeRegularFixedRewardSheet_V2",
            "StakeRegularRewardSheet_V5")]
        public void TryMigrate_Return_True_With_StakeState(
            long startedBlockIndex,
            long? receivedBlockIndex,
            string stakeRegularFixedRewardSheetTableName,
            string stakeRegularRewardSheetTableName)
        {
            IAccount state = new MockStateDelta();
            state = state.SetState(
                Addresses.GameConfig,
                new GameConfigState(GameConfigSheetFixtures.Default).Serialize());
            var stakeAddr = new PrivateKey().ToAddress();
            var stakeState = new StakeState(stakeAddr, startedBlockIndex);
            if (receivedBlockIndex is not null)
            {
                stakeState.Claim(receivedBlockIndex.Value);
            }

            state = state.SetState(stakeAddr, stakeState.Serialize());
            Assert.True(StakeStateUtils.TryMigrate(state, stakeAddr, out var stakeStateV2));
            Assert.Equal(
                stakeRegularFixedRewardSheetTableName,
                stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName);
            Assert.Equal(
                stakeRegularRewardSheetTableName,
                stakeStateV2.Contract.StakeRegularRewardSheetTableName);
            Assert.Equal(stakeState.StartedBlockIndex, stakeStateV2.StartedBlockIndex);
            Assert.Equal(stakeState.ReceivedBlockIndex, stakeStateV2.ReceivedBlockIndex);
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(0, long.MaxValue)]
        [InlineData(long.MaxValue, null)]
        [InlineData(long.MaxValue, long.MaxValue)]
        public void TryMigrate_Return_True_With_StakeStateV2(
            long startedBlockIndex,
            long? receivedBlockIndex)
        {
            IAccount state = new MockStateDelta();
            state = state.SetState(
                Addresses.GameConfig,
                new GameConfigState(GameConfigSheetFixtures.Default).Serialize());
            var stakeAddr = new PrivateKey().ToAddress();
            var stakePolicySheet = new StakePolicySheet();
            stakePolicySheet.Set(StakePolicySheetFixtures.V2);
            var contract = new Contract(stakePolicySheet);
            var stakeStateV2 = receivedBlockIndex is null
                ? new StakeStateV2(contract, startedBlockIndex)
                : new StakeStateV2(contract, receivedBlockIndex.Value);

            state = state.SetState(stakeAddr, stakeStateV2.Serialize());
            Assert.True(StakeStateUtils.TryMigrate(state, stakeAddr, out var result));
            Assert.Equal(stakeStateV2.Contract, result.Contract);
            Assert.Equal(stakeStateV2.StartedBlockIndex, result.StartedBlockIndex);
            Assert.Equal(stakeStateV2.ReceivedBlockIndex, result.ReceivedBlockIndex);
        }
    }
}
