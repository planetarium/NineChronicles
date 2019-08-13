using Nekoyume.Data;
using Nekoyume.Game.Item;
using Nekoyume.Game.Skill;
using System;
using UnityEngine;
using Material = Nekoyume.Game.Item.Material;

namespace Nekoyume.Game.Factory
{
    public class ItemFactory : MonoBehaviour
    {
        public static ItemBase CreateMaterial(int itemId, Guid guid, SkillBase skillBase = null)
        {
            Data.Table.Item itemData;
            if (Tables.instance.TryGetItem(itemId, out itemData))
            {
                return Create(itemData, guid, skillBase);
            }
            else
            {
                return null;
            }
        }

        public static ItemBase CreateEquipment(int itemId, Guid guid, SkillBase skillBase = null)
        {
            Data.Table.ItemEquipment itemData;
            if (Tables.instance.TryGetItemEquipment(itemId, out itemData))
            {
                return Create(itemData, guid, skillBase);
            }
            else
            {
                return null;
            }
        }

        public static ItemBase Create(Data.Table.Item itemData, Guid id, SkillBase skillBase = null)
        {
            var type = itemData.cls.ToEnumItemType();
            switch (type)
            {
                case ItemBase.ItemType.Material:
                    return new Material(itemData);
                case ItemBase.ItemType.Weapon:
                    return new Weapon(itemData, id, skillBase);
                case ItemBase.ItemType.RangedWeapon:
                    return new RangedWeapon(itemData, id, skillBase);
                case ItemBase.ItemType.Armor:
                    return new Armor(itemData, id, skillBase);
                case ItemBase.ItemType.Belt:
                    return new Belt(itemData, id, skillBase);
                case ItemBase.ItemType.Necklace:
                    return new Necklace(itemData, id, skillBase);
                case ItemBase.ItemType.Ring:
                    return new Ring(itemData, id, skillBase);
                case ItemBase.ItemType.Helm:
                    return new Helm(itemData, id, skillBase);
                case ItemBase.ItemType.Set:
                    return new SetItem(itemData, id, skillBase);
                case ItemBase.ItemType.Food:
                    return new Food(itemData, id, skillBase);
                case ItemBase.ItemType.Shoes:
                    return new Shoes(itemData, id, skillBase);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
