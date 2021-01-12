namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Xunit;

    public static class Doomfist
    {
        public static Equipment Get(TableSheets tableSheets)
        {
            var row = tableSheets.EquipmentItemSheet.OrderedList.Aggregate((row1, row2) =>
            {
                var row1Atk = row1.Stat.Type == StatType.ATK
                    ? row1.Stat.Value
                    : 0;
                var row2Atk = row2.Stat.Type == StatType.ATK
                    ? row2.Stat.Value
                    : 0;
                return row1Atk > row2Atk
                    ? row1
                    : row2;
            });
            Assert.NotNull(row);
            return (Equipment)ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 0);
        }
    }
}
