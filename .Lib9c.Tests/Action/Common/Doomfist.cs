namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Xunit;

    public static class Doomfist
    {
        public static Equipment GetWeapon(TableSheets tableSheets)
        {
            var row = tableSheets.EquipmentItemSheet.OrderedList
                .Where(e => e.ItemSubType == ItemSubType.Weapon)
                .Aggregate((row1, row2) =>
                {
                    var row1Value = row1.Stat.Type == StatType.ATK
                        ? row1.Stat.Value
                        : 0;
                    var row2Value = row2.Stat.Type == StatType.ATK
                        ? row2.Stat.Value
                        : 0;
                    return row1Value > row2Value
                        ? row1
                        : row2;
                });
            Assert.NotNull(row);
            return (Equipment)ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 0, 10);
        }

        public static Equipment GetArmor(TableSheets tableSheets)
        {
            var row = tableSheets.EquipmentItemSheet.OrderedList
                .Where(e => e.ItemSubType == ItemSubType.Armor)
                .Aggregate((row1, row2) =>
                {
                    var row1Value = row1.Stat.Type == StatType.DEF
                        ? row1.Stat.Value
                        : 0;
                    var row2Value = row2.Stat.Type == StatType.DEF
                        ? row2.Stat.Value
                        : 0;
                    return row1Value > row2Value
                        ? row1
                        : row2;
                });
            Assert.NotNull(row);
            return (Equipment)ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 0, 10);
        }

        public static List<Equipment> GetAllParts(TableSheets tableSheets) => new List<Equipment>
        {
            GetWeapon(tableSheets),
            GetArmor(tableSheets),
        };
    }
}
