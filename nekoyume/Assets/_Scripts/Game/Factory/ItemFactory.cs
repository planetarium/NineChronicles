using Nekoyume.Data;
using Nekoyume.Game.Item;
using System;
using UnityEngine;
using Material = Nekoyume.Game.Item.Material;

namespace Nekoyume.Game.Factory
{
    public class ItemFactory : MonoBehaviour
    {
        public static ItemBase CreateMaterial(int itemId, Guid guid, Skill skill = null)
        {
            Data.Table.Item itemData;
            if (Tables.instance.TryGetItem(itemId, out itemData))
            {
                return Create(itemData, guid, skill);
            }
            else
            {
                return null;
            }
        }

        public static ItemBase CreateEquipment(int itemId, Guid guid, Skill skill = null)
        {
            Data.Table.ItemEquipment itemData;
            if (Tables.instance.TryGetItemEquipment(itemId, out itemData))
            {
                return Create(itemData, guid, skill);
            }
            else
            {
                return null;
            }
        }

        public static ItemBase Create(Data.Table.Item itemData, Guid id, Skill skill = null)
        {
            var type = itemData.cls.ToEnumItemType();
            switch (type)
            {
                case ItemBase.ItemType.Material:
                    return new Material(itemData);
                case ItemBase.ItemType.Weapon:
                    return new Weapon(itemData, id, skill);
                case ItemBase.ItemType.RangedWeapon:
                    return new RangedWeapon(itemData, id, skill);
                case ItemBase.ItemType.Armor:
                    return new Armor(itemData, id, skill);
                case ItemBase.ItemType.Belt:
                    return new Belt(itemData, id, skill);
                case ItemBase.ItemType.Necklace:
                    return new Necklace(itemData, id, skill);
                case ItemBase.ItemType.Ring:
                    return new Ring(itemData, id, skill);
                case ItemBase.ItemType.Helm:
                    return new Helm(itemData, id, skill);
                case ItemBase.ItemType.Set:
                    return new SetItem(itemData, id, skill);
                case ItemBase.ItemType.Food:
                    return new Food(itemData, id, skill);
                case ItemBase.ItemType.Shoes:
                    return new Shoes(itemData, id, skill);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
