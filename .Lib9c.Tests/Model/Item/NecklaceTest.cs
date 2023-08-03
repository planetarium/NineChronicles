namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class NecklaceTest
    {
        private readonly EquipmentItemSheet.Row _necklaceRow;

        public NecklaceTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _necklaceRow = tableSheets.EquipmentItemSheet.OrderedList.FirstOrDefault(row =>
                row.ItemSubType == ItemSubType.Necklace);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_necklaceRow);

            var costume = new Necklace(_necklaceRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Necklace((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(costume, deserialized);
        }
    }
}
