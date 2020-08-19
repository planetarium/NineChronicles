namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.TableData;
    using Xunit;

    public class SimulatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;

        public SimulatorTest()
        {
            _tableSheets = TableSheets.FromTableSheetsState(TableSheetsImporter.ImportTableSheets());
            _random = new ItemEnhancementTest.TestRandom();
        }

        [Fact]
        public void SetRewardAll()
        {
            var row = _tableSheets.StageSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "5", "100", "0", "0", "0", "0", "0", "0", "chapter_1_1", "bgm_stage_green", "306043", "1", "1",
                "1", "303000", "0.01", "1", "1", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, "2", "2",
            });
            var itemSelector = StageSimulator.SetItemSelector(row, _random);
            var reward = Simulator.SetReward(itemSelector, _random.Next(2, 2), _random, _tableSheets);
            Assert.Equal(2, reward.Count);
            Assert.NotEmpty(reward);
            Assert.Equal(new[] { 306043, 303000 }, reward.Select(i => i.Id).ToArray());
        }

        [Fact]
        public void SetRewardDuplicateItem()
        {
            var row = _tableSheets.StageSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "5", "100", "0", "0", "0", "0", "0", "0", "chapter_1_1", "bgm_stage_green", "306043", "1", "2",
                "2", "303000", "0.01", "2", "2", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, "2", "2",
            });
            var itemSelector = StageSimulator.SetItemSelector(row, _random);
            var reward = Simulator.SetReward(itemSelector, _random.Next(2, 2), _random, _tableSheets);
            Assert.Equal(2, reward.Count);
            Assert.NotEmpty(reward);
            Assert.Single(reward.Select(i => i.Id).ToImmutableHashSet());
        }

        [Fact]
        public void SetRewardLimitByStageDrop()
        {
            var row = _tableSheets.StageSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "5", "100", "0", "0", "0", "0", "0", "0", "chapter_1_1", "bgm_stage_green", "306043", "1", "2",
                "2", "303000", "0.01", "2", "2", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, "1", "1",
            });
            var itemSelector = StageSimulator.SetItemSelector(row, _random);
            var reward = Simulator.SetReward(itemSelector, _random.Next(1, 1), _random, _tableSheets);
            Assert.Single(reward);
        }

        [Fact]
        public void SetRewardLimitByItemDrop()
        {
            var row = _tableSheets.StageSheet.Values.First();
            row.Set(new List<string>
            {
                "1", "5", "100", "0", "0", "0", "0", "0", "0", "chapter_1_1", "bgm_stage_green", "306043", "1", "1",
                "1", "303000", "0.01", "1", "1", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, "1", "4",
            });
            var itemSelector = StageSimulator.SetItemSelector(row, _random);
            var reward = Simulator.SetReward(itemSelector, _random.Next(1, 4), _random, _tableSheets);
            Assert.True(reward.Count <= 2);
            Assert.NotEmpty(reward);
        }
    }
}
