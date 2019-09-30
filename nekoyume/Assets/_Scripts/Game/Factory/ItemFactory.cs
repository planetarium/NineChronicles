using Nekoyume.Data;
using Nekoyume.Game.Item;
using System;
using Nekoyume.EnumType;
using Nekoyume.TableData;
using UnityEngine;
using Material = Nekoyume.Game.Item.Material;

namespace Nekoyume.Game.Factory
{
    public class ItemFactory : MonoBehaviour
    {
        public static ItemBase CreateMaterial(int itemId, Guid guid = default)
        {
            return !Game.instance.TableSheets.MaterialItemSheet.TryGetValue(itemId, out var itemData)
                ? null
                : Create(itemData, guid);
        }

        public static ItemBase CreateEquipment(int itemId, Guid guid)
        {
            return Game.instance.TableSheets.EquipmentItemSheet.TryGetValue(itemId, out var itemData)
                ? Create(itemData, guid)
                : null;
        }

        public static ItemBase Create(ItemSheet.Row itemRow, Guid id)
        {
            switch (itemRow.ItemSubType)
            {
                // Consumable
                case ItemSubType.Food:
                    return new Consumable((ConsumableItemSheet.Row) itemRow, id);
                // Equipment
                case ItemSubType.Weapon:
                    return new Weapon((EquipmentItemSheet.Row) itemRow, id);
                case ItemSubType.RangedWeapon:
                    return new RangedWeapon((EquipmentItemSheet.Row) itemRow, id);
                case ItemSubType.Armor:
                    return new Armor((EquipmentItemSheet.Row) itemRow, id);
                case ItemSubType.Belt:
                    return new Belt((EquipmentItemSheet.Row) itemRow, id);
                case ItemSubType.Necklace:
                    return new Necklace((EquipmentItemSheet.Row) itemRow, id);
                case ItemSubType.Ring:
                    return new Ring((EquipmentItemSheet.Row) itemRow, id);
                case ItemSubType.Helm:
                    return new Helm((EquipmentItemSheet.Row) itemRow, id);
                case ItemSubType.Set:
                    return new SetItem((EquipmentItemSheet.Row) itemRow, id);
                case ItemSubType.Shoes:
                    return new Shoes((EquipmentItemSheet.Row) itemRow, id);
                // Material
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                    return new Material((MaterialItemSheet.Row) itemRow);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
