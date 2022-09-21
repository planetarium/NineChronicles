namespace Lib9c.Tests.TableData
{
    using System;
    using Nekoyume.Extensions;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldBossListSheetTest
    {
        private const string Csv = @"id,boss_id,started_block_index,ended_block_index,fee,ticket_price,additional_ticket_price,max_purchase_count
1,205005,0,100,300,200,100,10
2,203007,200,300,300,200,100,10
";

        [Fact]
        public void FindRowByBlockIndex()
        {
            var sheet = new WorldBossListSheet();
            sheet.Set(Csv);

            Assert.NotNull(sheet.FindRowByBlockIndex(0));
            Assert.NotNull(sheet.FindRowByBlockIndex(1));
            Assert.NotNull(sheet.FindRowByBlockIndex(50));
            Assert.NotNull(sheet.FindRowByBlockIndex(100));

            Assert.Throws<InvalidOperationException>(() => sheet.FindRowByBlockIndex(-1));
            Assert.Throws<InvalidOperationException>(() => sheet.FindRowByBlockIndex(400));
        }

        [Fact]
        public void FindRaidIdByBlockIndex()
        {
            var sheet = new WorldBossListSheet();
            sheet.Set(Csv);

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
            var sheet = new WorldBossListSheet();
            sheet.Set(Csv);

            Assert.Equal(1, sheet.FindPreviousRaidIdByBlockIndex(150));
            Assert.Equal(2, sheet.FindPreviousRaidIdByBlockIndex(350));
            Assert.Throws<InvalidOperationException>(() =>
                sheet.FindPreviousRaidIdByBlockIndex(100));
        }
    }
}
