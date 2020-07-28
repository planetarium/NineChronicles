namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EquipmentTest
    {
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
