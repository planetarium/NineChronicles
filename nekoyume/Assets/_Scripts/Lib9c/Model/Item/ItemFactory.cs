using System;
using System.Globalization;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.TableData;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    public static class ItemFactory
    {
        public static Costume CreateCostume(CostumeItemSheet.Row row)
        {
            return new Costume(row);
        }

        public static Material CreateMaterial(MaterialItemSheet sheet, int itemId)
        {
            return !sheet.TryGetValue(itemId, out var itemData)
                ? null
                : CreateMaterial(itemData);
        }

        public static Material CreateMaterial(MaterialItemSheet.Row row)
        {
            return new Material(row);
        }

        public static ItemUsable CreateItemUsable(ItemSheet.Row itemRow, Guid id,
            long requiredBlockIndex)
        {
            switch (itemRow.ItemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new Consumable((ConsumableItemSheet.Row) itemRow, id,
                        requiredBlockIndex);
                // Equipment
                case ItemSubType.Weapon:
                    return new Weapon((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                case ItemSubType.Armor:
                    return new Armor((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                case ItemSubType.Belt:
                    return new Belt((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                case ItemSubType.Necklace:
                    return new Necklace((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                case ItemSubType.Ring:
                    return new Ring((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                default:
                    throw new ArgumentOutOfRangeException(
                        itemRow.Id.ToString(CultureInfo.InvariantCulture));
            }
        }
        private static ItemBase DeserializeLegacy(Dictionary serialized)
        {
            var data = (Dictionary) serialized["data"];
            var row = DeserializeRow(data);
            switch (row)
            {
                case CostumeItemSheet.Row costumeRow:
                    var costume = CreateCostume(costumeRow);
                    if (serialized.TryGetValue((Text) "equipped", out var costumeEquipped))
                    {
                        costume.equipped = costumeEquipped.ToBoolean();
                    }

                    return costume;
                // 기존체인 호환성을 위해 이전 코드를 남겨둠.
                case MaterialItemSheet.Row materialRow:
                    return CreateMaterial(materialRow);
            }

            var itemUsable = CreateItemUsable(
                row,
                serialized.GetGuid("itemId"),
                serialized.GetLong("requiredBlockIndex")
            );
            if (itemUsable is null)
            {
                return null;
            }

            if (serialized.TryGetValue((Text) "statsMap", out var statsMap) &&
                serialized.TryGetValue((Text) "skills", out var skills))
            {
                itemUsable.StatsMap.Deserialize((Dictionary) statsMap);
                foreach (var skill in (List) skills)
                {
                    itemUsable.Skills.Add(SkillFactory.Deserialize((Dictionary) skill));
                }
            }

            if (serialized.TryGetValue((Text) "buffSkills", out var buffSkills))
            {
                foreach (var buffSkill in (List) buffSkills)
                {
                    itemUsable.BuffSkills.Add(
                        (BuffSkill) SkillFactory.Deserialize((Dictionary) buffSkill));
                }
            }

            if (!(itemUsable is Equipment equipment))
            {
                return itemUsable;
            }

            if (serialized.TryGetValue((Text) "equipped", out var equipped))
            {
                equipment.equipped = ((Bencodex.Types.Boolean) equipped).Value;
            }

            if (serialized.TryGetValue((Text) "level", out var level))
            {
                equipment.level = (int) ((Integer) level).Value;
            }

            return equipment;
        }

        public static ItemBase Deserialize(Dictionary serialized)
        {
            if (serialized.TryGetValue((Text) "data", out _))
            {
                return DeserializeLegacy(serialized);
            }

            if (serialized.TryGetValue((Text) "item_type", out var type))
            {
                var itemType = type.ToEnum<ItemType>();
                switch (itemType)
                {
                    case ItemType.Consumable:
                        return new Consumable(serialized);
                    case ItemType.Costume:
                        return new Costume(serialized);
                    case ItemType.Equipment:
                        if (serialized.TryGetValue((Text) "item_sub_type", out var subType))
                        {
                            var itemSubType = subType.ToEnum<ItemSubType>();
                            switch (itemSubType)
                            {
                                case ItemSubType.Weapon:
                                    return new Weapon(serialized);
                                case ItemSubType.Armor:
                                    return new Armor(serialized);
                                case ItemSubType.Belt:
                                    return new Belt(serialized);
                                case ItemSubType.Necklace:
                                    return new Necklace(serialized);
                                case ItemSubType.Ring:
                                    return new Ring(serialized);
                            }
                        }
                        break;
                    case ItemType.Material:
                        return new Material(serialized);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(itemType));
                }
            }

            throw new ArgumentException($"Can't Deserialize Item {serialized}");
        }
        private static ItemSheet.Row DeserializeRow(Dictionary serialized)
        {
            var itemSubType =
                (ItemSubType) Enum.Parse(typeof(ItemSubType), (Text) serialized["item_sub_type"]);
            switch (itemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new ConsumableItemSheet.Row(serialized);
                // Costume
                case ItemSubType.EarCostume:
                case ItemSubType.EyeCostume:
                case ItemSubType.FullCostume:
                case ItemSubType.HairCostume:
                case ItemSubType.TailCostume:
                case ItemSubType.Title:
                    return new CostumeItemSheet.Row(serialized);
                // Equipment
                case ItemSubType.Weapon:
                case ItemSubType.Armor:
                case ItemSubType.Belt:
                case ItemSubType.Necklace:
                case ItemSubType.Ring:
                    return new EquipmentItemSheet.Row(serialized);
                // Material
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                case ItemSubType.Chest:
                    return new MaterialItemSheet.Row(serialized);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
