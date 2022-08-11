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
                "id,_name,start_block_index,dungeon_end_block_index,dungeon_tickets_max,dungeon_tickets_reset_interval_block_range,dungeon_ticket_price,dungeon_ticket_additional_price,dungeon_exp_seed_value,recipe_end_block_index");
            sb.AppendLine("1001,\"2022 Summer Event\",0,100,8,5040,5,2,1,110");
            var csv = sb.ToString();

            var sheet = new EventScheduleSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(1001, row.Id);
            Assert.Equal(0, row.StartBlockIndex);
            Assert.Equal(100, row.DungeonEndBlockIndex);
            Assert.Equal(8, row.DungeonTicketsMax);
            Assert.Equal(5040, row.DungeonTicketsResetIntervalBlockRange);
            Assert.Equal(5, row.DungeonTicketPrice);
            Assert.Equal(2, row.DungeonTicketAdditionalPrice);
            Assert.Equal(1, row.DungeonExpSeedValue);
            Assert.Equal(110, row.RecipeEndBlockIndex);
        }
    }
}
