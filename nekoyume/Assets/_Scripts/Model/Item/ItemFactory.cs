using System;
using Bencodex.Types;
using Nekoyume.TableData;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    public static class ItemFactory
    {
        public static ItemBase CreateMaterial(MaterialItemSheet sheet, int itemId)
        {
            return !sheet.TryGetValue(itemId, out var itemData)
                ? null
                : Create(itemData, default);
        }

        public static ItemBase Create(ItemSheet.Row itemRow, Guid id)
        {
            switch (itemRow.ItemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new Consumable((ConsumableItemSheet.Row)itemRow, id);
                // Equipment
                case ItemSubType.Weapon:
                    return new Weapon((EquipmentItemSheet.Row)itemRow, id);
                case ItemSubType.RangedWeapon:
                    return new RangedWeapon((EquipmentItemSheet.Row)itemRow, id);
                case ItemSubType.Armor:
                    return new Armor((EquipmentItemSheet.Row)itemRow, id);
                case ItemSubType.Belt:
                    return new Belt((EquipmentItemSheet.Row)itemRow, id);
                case ItemSubType.Necklace:
                    return new Necklace((EquipmentItemSheet.Row)itemRow, id);
                case ItemSubType.Ring:
                    return new Ring((EquipmentItemSheet.Row)itemRow, id);
                case ItemSubType.Helm:
                    return new Helm((EquipmentItemSheet.Row)itemRow, id);
                case ItemSubType.Set:
                    return new SetItem((EquipmentItemSheet.Row)itemRow, id);
                case ItemSubType.Shoes:
                    return new Shoes((EquipmentItemSheet.Row)itemRow, id);
                // Material
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                    return new Material((MaterialItemSheet.Row)itemRow);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ItemBase Deserialize(Dictionary serialized)
        {
            var data = (Dictionary)serialized["data"];
            serialized.TryGetValue((Text)"itemId", out IValue id);
            var item = Create(
                DeserializeRow(data),
                id?.ToGuid() ?? default
            );
            if (item is ItemUsable itemUsable)
            {
                if (serialized.TryGetValue((Text)"statsMap", out var statsMap) &&
                    serialized.TryGetValue((Text)"skills", out var skills))
                {
                    itemUsable.StatsMap.Deserialize((Dictionary)statsMap);
                    foreach (var skill in (List)skills)
                    {
                        itemUsable.Skills.Add(SkillFactory.Deserialize((Dictionary)skill));
                    }
                }

                if (serialized.TryGetValue((Text)"buffSkills", out var buffSkills))
                {
                    foreach (var buffSkill in (List)buffSkills)
                    {
                        itemUsable.BuffSkills.Add((BuffSkill)SkillFactory.Deserialize((Dictionary)buffSkill));
                    }
                }

                if (itemUsable is Equipment equipment)
                {
                    if (serialized.TryGetValue((Text)"equipped", out var equipped))
                    {
                        equipment.equipped = ((Bencodex.Types.Boolean)equipped).Value;
                    }
                    if (serialized.TryGetValue((Text)"level", out var level))
                    {
                        equipment.level = (int)((Integer)level).Value;
                    }
                }
            }

            return item;
        }

        private static ItemSheet.Row DeserializeRow(Dictionary serialized)
        {
            var itemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), (Text)serialized["item_sub_type"]);
            switch (itemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new ConsumableItemSheet.Row(serialized);
                // Equipment
                case ItemSubType.Weapon:
                case ItemSubType.RangedWeapon:
                case ItemSubType.Armor:
                case ItemSubType.Belt:
                case ItemSubType.Necklace:
                case ItemSubType.Ring:
                case ItemSubType.Helm:
                case ItemSubType.Set:
                case ItemSubType.Shoes:
                    return new EquipmentItemSheet.Row(serialized);
                // Material
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                    return new MaterialItemSheet.Row(serialized);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
