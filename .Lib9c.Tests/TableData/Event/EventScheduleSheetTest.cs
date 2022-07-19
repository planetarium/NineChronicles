namespace Lib9c.Tests.TableData.Event
{
    using System.Text;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventScheduleSheetTest
    {
        [Fact]
        public void Set()
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "id,name,start_block_index,dungeon_end_block_index,dungeon_tickets_max,dungeon_tickets_reset_interval_block_range,dungeon_exp_seed_value,recipe_end_block_index");
            sb.AppendLine("10000001,\"2022 Summer Event\",0,100,8,5040,10,110");
            var csv = sb.ToString();

            var sheet = new EventScheduleSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(10000001, row.Id);
            Assert.Equal("\"2022 Summer Event\"", row.Name);
            Assert.Equal(0, row.StartBlockIndex);
            Assert.Equal(100, row.DungeonEndBlockIndex);
            Assert.Equal(8, row.DungeonTicketsMax);
            Assert.Equal(5040, row.DungeonTicketsResetIntervalBlockRange);
            Assert.Equal(10, row.DungeonExpSeedValue);
            Assert.Equal(110, row.RecipeEndBlockIndex);
        }
    }
}
