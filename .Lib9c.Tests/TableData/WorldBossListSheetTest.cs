namespace Lib9c.Tests.TableData
{
    using System;
    using Nekoyume.Extensions;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldBossListSheetTest
    {
        [Fact]
        public void FindRowByBlockIndex()
        {
            const string csv = @"id,boss_id,started_block_index,ended_block_index
            1,205005,0,100
            ";

            var sheet = new WorldBossListSheet();
            sheet.Set(csv);

            Assert.NotNull(sheet.FindRowByBlockIndex(0));
            Assert.NotNull(sheet.FindRowByBlockIndex(1));
            Assert.NotNull(sheet.FindRowByBlockIndex(50));
            Assert.NotNull(sheet.FindRowByBlockIndex(100));

            Assert.Throws<InvalidOperationException>(() => sheet.FindRowByBlockIndex(-1));
            Assert.Throws<InvalidOperationException>(() => sheet.FindRowByBlockIndex(200));
        }

        [Fact]
        public void FindRaidIdByBlockIndex()
        {
            const string csv = @"id,boss_id,started_block_index,ended_block_index
            1,205005,0,100
            2,205005,200,300
            ";

            var sheet = new WorldBossListSheet();
            sheet.Set(csv);

            Assert.Equal(1, sheet.FindRaidIdByBlockIndex(0));
            Assert.Equal(1, sheet.FindRaidIdByBlockIndex(1));
            Assert.Equal(1, sheet.FindRaidIdByBlockIndex(50));
            Assert.Equal(1, sheet.FindRaidIdByBlockIndex(100));
            Assert.Equal(2, sheet.FindRaidIdByBlockIndex(200));
            Assert.Equal(2, sheet.FindRaidIdByBlockIndex(250));
            Assert.Equal(2, sheet.FindRaidIdByBlockIndex(300));
            Assert.Throws<InvalidOperationException>(() => sheet.FindRaidIdByBlockIndex(301));
        }

        [Fact]
        public void FindPreviousRaidIdByBlockIndex()
        {
            const string csv = @"id,boss_id,started_block_index,ended_block_index
            1,205005,0,100
            2,205005,200,300
            ";

            var sheet = new WorldBossListSheet();
            sheet.Set(csv);

            Assert.Equal(1, sheet.FindPreviousRaidIdByBlockIndex(150));
            Assert.Equal(2, sheet.FindPreviousRaidIdByBlockIndex(350));
            Assert.Throws<InvalidOperationException>(() =>
                sheet.FindPreviousRaidIdByBlockIndex(100));
        }
    }
}
