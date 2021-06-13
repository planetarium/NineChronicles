namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Model.State;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class MonsterCollectionStateTest
    {
        private readonly Address _address;
        private readonly TableSheets _tableSheets;

        public MonsterCollectionStateTest()
        {
            _address = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            var monsterCollectionState = new MonsterCollectionState(_address, 1, 10000, _tableSheets.MonsterCollectionRewardSheet);
            var serialized = (Dictionary)monsterCollectionState.Serialize();
            Assert.Equal(serialized, new MonsterCollectionState(serialized).Serialize());
        }

        [Fact]
        public void Serialize_DotNet_API()
        {
            var monsterCollectionState = new MonsterCollectionState(_address, 1, 10000, _tableSheets.MonsterCollectionRewardSheet);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, monsterCollectionState);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (MonsterCollectionState)formatter.Deserialize(ms);

            Assert.Equal(monsterCollectionState.Serialize(), deserialized.Serialize());
        }

        [Theory]
        [InlineData(0, 0, MonsterCollectionState.RewardInterval, 1)]
        [InlineData(0, MonsterCollectionState.RewardInterval, MonsterCollectionState.RewardInterval * 2, 1)]
        [InlineData(0, MonsterCollectionState.RewardInterval, MonsterCollectionState.RewardInterval * 3, 2)]
        [InlineData(0, MonsterCollectionState.RewardInterval, MonsterCollectionState.RewardInterval + 1, 0)]
        [InlineData(MonsterCollectionState.RewardInterval, 0, MonsterCollectionState.RewardInterval * 1.5, 0)]
        public void CalulateStep(long startedBlockIndex, long receivedBlockIndex, long blockIndex, int expectedStep)
        {
            var monsterCollectionState = new MonsterCollectionState(_address, 1, startedBlockIndex, _tableSheets.MonsterCollectionRewardSheet);
            var serialized = (Dictionary)monsterCollectionState.Serialize();
            serialized = serialized.SetItem(ReceivedBlockIndexKey, receivedBlockIndex.Serialize());
            monsterCollectionState = new MonsterCollectionState(serialized);
            Assert.Equal(expectedStep, monsterCollectionState.CalculateStep(blockIndex));
        }

        [Theory]
        [InlineData(MonsterCollectionState.LockUpInterval - 1, true)]
        [InlineData(MonsterCollectionState.LockUpInterval, false)]
        [InlineData(MonsterCollectionState.LockUpInterval + 1, false)]
        public void IsLocked(long blockIndex, bool expected)
        {
            var monsterCollectionState = new MonsterCollectionState(_address, 1, 0);
            Assert.Equal(expected, monsterCollectionState.IsLocked(blockIndex));
        }
    }
}
