namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Xunit;

    public static class Doomfist
    {
        public static Equipment GetOne(
            TableSheets tableSheets,
            int avatarLevel = 1,
            ItemSubType itemSubType = ItemSubType.Weapon,
            ElementalType? elementalType = null)
        {
            var statType = itemSubType switch
            {
                ItemSubType.Armor => StatType.HP,
                ItemSubType.Belt => StatType.SPD,
                ItemSubType.Necklace => StatType.HIT,
                ItemSubType.Ring => StatType.DEF,
                ItemSubType.Weapon => StatType.ATK,
                _ => StatType.NONE,
            };
            if (statType == StatType.NONE)
            {
                return null;
            }

            var requirementSheet = tableSheets.ItemRequirementSheet;
            var row = tableSheets.EquipmentItemSheet.OrderedList
                .Where(e =>
                    e.ItemSubType == itemSubType &&
                    (!elementalType.HasValue || e.ElementalType == elementalType.Value) &&
                    requirementSheet.TryGetValue(e.Id, out var requirementRow) &&
                    avatarLevel >= requirementRow.Level)
                .Aggregate((row1, row2) =>
                {
                    var row1Value = row1.Stat.Type == statType
                        ? row1.Stat.Value
                        : 0;
                    var row2Value = row2.Stat.Type == statType
                        ? row2.Stat.Value
                        : 0;
                    return row1Value > row2Value
                        ? row1
                        : row2;
                });
            Assert.NotNull(row);
            return (Equipment)ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 0, 10);
        }

        public static List<Equipment> GetAllParts(
            TableSheets tableSheets,
            int avatarLevel = 1,
            ElementalType? elementalType = null) => new List<Equipment>
        {
            GetOne(tableSheets, avatarLevel, ItemSubType.Armor, elementalType),
            GetOne(tableSheets, avatarLevel, ItemSubType.Belt, elementalType),
            GetOne(tableSheets, avatarLevel, ItemSubType.Necklace, elementalType),
            GetOne(tableSheets, avatarLevel, ItemSubType.Ring, elementalType),
            GetOne(tableSheets, avatarLevel, ItemSubType.Weapon, elementalType),
        };
    }
}
