namespace Lib9c.Tests.Model.State
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class StakingStateTest
    {
        private readonly Address _address;
        private readonly TableSheets _tableSheets;

        public StakingStateTest()
        {
            _address = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
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

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Update(long rewardLevel)
        {
            StakingState stakingState = new StakingState(_address, 1, 10000);
            Assert.Equal(1, stakingState.Level);
            Assert.Equal(10000, stakingState.StartedBlockIndex);
            Assert.Equal(170000, stakingState.ExpiredBlockIndex);

            stakingState.Update(2, rewardLevel);
            Assert.Equal(2, stakingState.Level);
            for (long i = rewardLevel; i < 4; i++)
            {
                Assert.Equal(2, stakingState.RewardLevelMap[i + 1]);
            }
        }

        [Fact]
        public void UpdateRewardMap()
        {
            StakingState stakingState = new StakingState(_address, 1, 10000);
            Assert.Empty(stakingState.RewardMap);

            Address avatarAddress = default;
            List<StakingRewardSheet.RewardInfo> rewards = _tableSheets.StakingRewardSheet[1].Rewards;
            StakingResult result = new StakingResult(Guid.NewGuid(), avatarAddress, rewards);
            stakingState.UpdateRewardMap(1, result, 14000);
            Assert.Single(stakingState.RewardMap);
            Assert.Equal(result, stakingState.RewardMap[1]);
            Assert.Equal(14000, stakingState.ReceivedBlockIndex);
            Assert.Equal(1, stakingState.RewardLevel);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void UpdateRewardMap_Throw_ArgumentOutOfRangeException(long rewardLevel)
        {
            StakingState stakingState = new StakingState(_address, 1, 10000);

            List<StakingRewardSheet.RewardInfo> rewards = _tableSheets.StakingRewardSheet[1].Rewards;
            StakingResult result = new StakingResult(Guid.NewGuid(), _address, rewards);
            Assert.Throws<ArgumentOutOfRangeException>(() => stakingState.UpdateRewardMap(rewardLevel, result, 0));
        }

        [Fact]
        public void UpdateRewardMap_Throw_AlreadyReceivedException()
        {
            StakingState stakingState = new StakingState(_address, 1, 10000);
            List<StakingRewardSheet.RewardInfo> rewards = _tableSheets.StakingRewardSheet[1].Rewards;
            StakingResult result = new StakingResult(Guid.NewGuid(), default, rewards);
            stakingState.UpdateRewardMap(1, result, 14000);
            Assert.Throws<AlreadyReceivedException>(() => stakingState.UpdateRewardMap(1, result, 0));
        }

        [Fact]
        public void GetRewardLevel()
        {
            StakingState stakingState = new StakingState(_address, 1, 0);
            for (long i = 0; i < StakingState.RewardCapacity; i++)
            {
                Assert.Equal(i, stakingState.GetRewardLevel(i * StakingState.RewardInterval));
            }
        }
    }
}
