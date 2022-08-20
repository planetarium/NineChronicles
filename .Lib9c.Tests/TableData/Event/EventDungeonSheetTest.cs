namespace Lib9c.Tests.TableData.Event
{
    using System.Text;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventDungeonSheetTest
    {
        [Fact]
        public void Set()
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,name,stage_begin,stage_end");
            sb.AppendLine("10010001,\"Event Dungeon For 1001\",10010001,10010020");
            var csv = sb.ToString();

            var sheet = new EventDungeonSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(10010001, row.Id);
            Assert.Equal("\"Event Dungeon For 1001\"", row.Name);
            Assert.Equal(10010001, row.StageBegin);
            Assert.Equal(10010020, row.StageEnd);
        }
    }
}
