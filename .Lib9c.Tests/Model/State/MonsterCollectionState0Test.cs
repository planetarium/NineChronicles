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
    using static Lib9c.SerializeKeys;

    public class MonsterCollectionState0Test
    {
        private readonly Address _address;
        private readonly TableSheets _tableSheets;

        public MonsterCollectionState0Test()
        {
            _address = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 10000, _tableSheets.MonsterCollectionRewardSheet);
            Dictionary serialized = (Dictionary)monsterCollectionState.Serialize();
            Assert.Equal(serialized, new MonsterCollectionState0(serialized).Serialize());
        }

        [Fact]
        public void Serialize_DotNet_API()
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 10000, _tableSheets.MonsterCollectionRewardSheet);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, monsterCollectionState);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (MonsterCollectionState0)formatter.Deserialize(ms);

            Assert.Equal(monsterCollectionState.Serialize(), deserialized.Serialize());
        }

        [Fact]
        public void SerializeV2()
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 10000);
            Dictionary serialized = (Dictionary)monsterCollectionState.SerializeV2();
            Assert.Equal(serialized, new MonsterCollectionState0(serialized).SerializeV2());
        }

        [Fact]
        public void SerializeV2_DotNet_API()
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 10000);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, monsterCollectionState);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (MonsterCollectionState0)formatter.Deserialize(ms);

            Assert.Equal(monsterCollectionState.SerializeV2(), deserialized.SerializeV2());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Update(long rewardLevel)
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 10000, _tableSheets.MonsterCollectionRewardSheet);
            Assert.Equal(1, monsterCollectionState.Level);
            Assert.Equal(10000, monsterCollectionState.StartedBlockIndex);
            Assert.Equal(MonsterCollectionState0.RewardInterval * 4 + 10000, monsterCollectionState.ExpiredBlockIndex);

            monsterCollectionState.Update(2, rewardLevel, _tableSheets.MonsterCollectionRewardSheet);
            Assert.Equal(2, monsterCollectionState.Level);
            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet[2].Rewards;
            for (long i = rewardLevel; i < 4; i++)
            {
                Assert.Equal(rewards, monsterCollectionState.RewardLevelMap[i + 1]);
            }
        }

        [Fact]
        public void UpdateRewardMap()
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 10000, _tableSheets.MonsterCollectionRewardSheet);
            Assert.Empty(monsterCollectionState.RewardMap);

            Address avatarAddress = default;
            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet[1].Rewards;
            MonsterCollectionResult result = new MonsterCollectionResult(Guid.NewGuid(), avatarAddress, rewards);
            monsterCollectionState.UpdateRewardMap(1, result, 14000);
            Assert.Single(monsterCollectionState.RewardMap);
            Assert.Equal(result, monsterCollectionState.RewardMap[1]);
            Assert.Equal(14000, monsterCollectionState.ReceivedBlockIndex);
            Assert.Equal(1, monsterCollectionState.RewardLevel);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void UpdateRewardMap_Throw_ArgumentOutOfRangeException(long rewardLevel)
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 10000, _tableSheets.MonsterCollectionRewardSheet);

            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet[1].Rewards;
            MonsterCollectionResult result = new MonsterCollectionResult(Guid.NewGuid(), _address, rewards);
            Assert.Throws<ArgumentOutOfRangeException>(() => monsterCollectionState.UpdateRewardMap(rewardLevel, result, 0));
        }

        [Fact]
        public void UpdateRewardMap_Throw_AlreadyReceivedException()
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 10000, _tableSheets.MonsterCollectionRewardSheet);
            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet[1].Rewards;
            MonsterCollectionResult result = new MonsterCollectionResult(Guid.NewGuid(), default, rewards);
            monsterCollectionState.UpdateRewardMap(1, result, 14000);
            Assert.Throws<AlreadyReceivedException>(() => monsterCollectionState.UpdateRewardMap(1, result, 0));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 4)]
        public void GetRewardLevel(int interval, long expected)
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 0, _tableSheets.MonsterCollectionRewardSheet);
            long blockIndex = MonsterCollectionState0.RewardInterval * interval;
            Assert.Equal(expected, monsterCollectionState.GetRewardLevel(blockIndex));
        }

        [Theory]
        [InlineData(0, 0, MonsterCollectionState0.RewardInterval, true)]
        [InlineData(0, MonsterCollectionState0.RewardInterval, MonsterCollectionState0.RewardInterval * 2, true)]
        [InlineData(0, MonsterCollectionState0.RewardInterval, MonsterCollectionState0.RewardInterval + 1, false)]
        [InlineData(MonsterCollectionState0.RewardInterval, 0, MonsterCollectionState0.RewardInterval * 1.5, false)]
        public void CanReceive(long startedBlockIndex, long receivedBlockIndex, long blockIndex, bool expected)
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, startedBlockIndex, _tableSheets.MonsterCollectionRewardSheet);
            Dictionary serialized = (Dictionary)monsterCollectionState.Serialize();
            serialized = serialized.SetItem(ReceivedBlockIndexKey, receivedBlockIndex.Serialize());
            monsterCollectionState = new MonsterCollectionState0(serialized);
            Assert.Equal(receivedBlockIndex, monsterCollectionState.ReceivedBlockIndex);
            Assert.Equal(startedBlockIndex, monsterCollectionState.StartedBlockIndex);
            Assert.Equal(expected, monsterCollectionState.CanReceive(blockIndex));
        }

        [Theory]
        [InlineData(MonsterCollectionState0.LockUpInterval - 1, true)]
        [InlineData(MonsterCollectionState0.LockUpInterval, false)]
        [InlineData(MonsterCollectionState0.LockUpInterval + 1, false)]
        public void IsLock(long blockIndex, bool expected)
        {
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(_address, 1, 0);
            Assert.Equal(expected, monsterCollectionState.IsLock(blockIndex));
        }
    }
}
