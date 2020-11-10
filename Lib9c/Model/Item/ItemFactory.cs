using System;
using System.Collections.Generic;
using System.Globalization;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.TableData;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    public static class ItemFactory
    {
        public static ItemBase CreateItem(ItemSheet.Row row, IRandom random)
        {
            switch (row)
            {
                case CostumeItemSheet.Row costumeRow:
                    return CreateCostume(costumeRow, random.GenerateRandomGuid());
                case MaterialItemSheet.Row materialRow:
                    return CreateMaterial(materialRow);
                default:
                    return CreateItemUsable(row, random.GenerateRandomGuid(), 0);
            }
        }

        public static Costume CreateCostume(CostumeItemSheet.Row row, Guid itemId)
        {
            return new Costume(row, itemId);
        }

        public static Material CreateMaterial(MaterialItemSheet sheet, int itemId)
        {
            return !sheet.TryGetValue(itemId, out var itemData)
                ? null
                : CreateMaterial(itemData);
        }

        public static Material CreateMaterial(MaterialItemSheet.Row row)
        {
            return row.ItemSubType == ItemSubType.Chest
                ? CreateChest(row, null)
                : new Material(row);
        }

        public static ItemUsable CreateItemUsable(ItemSheet.Row itemRow, Guid id,
            long requiredBlockIndex, int level = 0)
        {
            Equipment equipment = null;

            switch (itemRow.ItemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new Consumable((ConsumableItemSheet.Row) itemRow, id,
                        requiredBlockIndex);
                // Equipment
                case ItemSubType.Weapon:
                    equipment = new Weapon((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                    break;
                case ItemSubType.Armor:
                    equipment = new Armor((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                    break;
                case ItemSubType.Belt:
                    equipment = new Belt((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                    break;
                case ItemSubType.Necklace:
                    equipment = new Necklace((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                    break;
                case ItemSubType.Ring:
                    equipment = new Ring((EquipmentItemSheet.Row) itemRow, id, requiredBlockIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        itemRow.Id.ToString(CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < level; ++i)
            {
                equipment.LevelUp();
            }

            return equipment;
        }

        public static Chest CreateChest(MaterialItemSheet.Row row, List<RedeemRewardSheet.RewardInfo> rewards)
        {
            return new Chest(row, rewards);
        }

        public static ItemBase Deserialize(Dictionary serialized)
        {
            if (serialized.TryGetValue((Text) "item_type", out var type) &&
                serialized.TryGetValue((Text) "item_sub_type", out var subType))
            {
                var itemType = type.ToEnum<ItemType>();
                var itemSubType = subType.ToEnum<ItemSubType>();

                switch (itemType)
                {
                    case ItemType.Consumable:
                        return new Consumable(serialized);
                    case ItemType.Costume:
                        return new Costume(serialized);
                    case ItemType.Equipment:
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
                        break;
                    case ItemType.Material:
                        return itemSubType == ItemSubType.Chest ? new Chest(serialized) : new Material(serialized);
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
