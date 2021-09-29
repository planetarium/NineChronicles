namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EquipmentTest
    {
        private readonly EquipmentItemSheet.Row _equipmentRow;

        public EquipmentTest()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _equipmentRow = tableSheets.EquipmentItemSheet.First;
        }

        public static Equipment CreateFirstEquipment(
            TableSheets tableSheets,
            Guid guid = default,
            long requiredBlockIndex = default)
        {
            var row = tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(row);

            return new Equipment(row, guid == default ? Guid.NewGuid() : guid, requiredBlockIndex);
        }

        [Fact]
        public void Serialize()
        {
            Assert.NotNull(_equipmentRow);

            var costume = new Equipment(_equipmentRow, Guid.NewGuid(), 0);
            var serialized = costume.Serialize();
            var deserialized = new Equipment((Bencodex.Types.Dictionary)serialized);
            var reSerialized = deserialized.Serialize();

            Assert.Equal(costume, deserialized);
            Assert.Equal(serialized, reSerialized);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            Assert.NotNull(_equipmentRow);

            var costume = new Equipment(_equipmentRow, Guid.NewGuid(), 0);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, costume);
            ms.Seek(0, SeekOrigin.Begin);
            var serialized = ms.ToArray();
            var deserialized = (Equipment)formatter.Deserialize(ms);
            ms.Seek(0, SeekOrigin.Begin);
            formatter.Serialize(ms, deserialized);
            var reSerialized = ms.ToArray();

            Assert.Equal(costume, deserialized);
            Assert.Equal(serialized, reSerialized);
        }

        [Fact]
        public void LevelUp()
        {
            var row = new EquipmentItemSheet.Row();
            row.Set(new List<string>() { "10100000", "Weapon", "0", "Normal", "0", "ATK", "1", "2", "10100000" });
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, 0);

            Assert.Equal(1m, equipment.StatsMap.ATK);
            equipment.LevelUp();
            Assert.Equal(2m, equipment.StatsMap.ATK);
        }
    }
}
