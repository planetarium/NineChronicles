namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Xunit;

    public static class Doomfist
    {
        public static readonly (int EquipmentSlotLevel, ItemSubType ItemSubType)[]
            OrderedEquipmentSlotLevelAndItemSubType
                =
                new (int EquipmentSlotLevel, ItemSubType ItemSubType)[]
                    {
                        (GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor, ItemSubType.Armor),
                        (GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt, ItemSubType.Belt),
                        (GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace, ItemSubType.Necklace),
                        (GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1, ItemSubType.Ring),
                        (GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2, ItemSubType.Ring),
                        (GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon, ItemSubType.Weapon),
                    }.OrderBy(tuple => tuple.EquipmentSlotLevel)
                    .ToArray();

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
            ElementalType? elementalType = null,
            int? totalCount = null)
        {
            var result = new List<Equipment>();
            var tuples = OrderedEquipmentSlotLevelAndItemSubType;
            var count = totalCount.HasValue ? Math.Min(totalCount.Value, tuples.Length) : tuples.Length;
            for (var i = 0; i < count; i++)
            {
                var (equipmentSlotLevel, itemSubType) = tuples[i];
                if (avatarLevel < equipmentSlotLevel)
                {
                    break;
                }

                result.Add(GetOne(tableSheets, avatarLevel, itemSubType, elementalType));
            }

            return result;
        }
    }
}
