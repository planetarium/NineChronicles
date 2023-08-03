namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class BeltTest
    {
        private readonly EquipmentItemSheet.Row _beltRow;

        public BeltTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _beltRow = tableSheets.EquipmentItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Belt);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_beltRow);

            var costume = new Belt(_beltRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Belt((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(costume, deserialized);
        }
    }
}
