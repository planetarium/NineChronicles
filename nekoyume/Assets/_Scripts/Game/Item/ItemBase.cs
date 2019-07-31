using System;
using Nekoyume.Data;
using Nekoyume.Game.Skill;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemBase
    {
        private const string ItemPath = "images/icon/item/{0}";
        private const string EquipmentPath = "images/icon/equipment/{0}";
        private const int DefaultId = 101000;

        protected bool Equals(ItemBase other)
        {
            return Equals(Data, other.Data);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemBase) obj);
        }

        public override int GetHashCode()
        {
            return (Data != null ? Data.GetHashCode() : 0);
        }

        public Data.Table.Item Data { get; }

        public ItemBase(Data.Table.Item data)
        {
            Data = data;
        }

        public enum ItemType
        {
            Material,
            Weapon,
            RangedWeapon,
            Armor,
            Belt,
            Necklace,
            Ring,
            Helm,
            Set,
            Food,
            Shoes,
        }

        public static ItemBase ItemFactory(Data.Table.Item itemData, Guid id, SkillBase skillBase = null)
        {
            var type = itemData.cls.ToEnumItemType();
            switch (type)
            {
                case ItemType.Material:
                    return new Material(itemData);
                case ItemType.Weapon:
                    return new Weapon(itemData, id, skillBase);
                case ItemType.RangedWeapon:
                    return new RangedWeapon(itemData, id, skillBase);
                case ItemType.Armor:
                    return new Armor(itemData, id, skillBase);
                case ItemType.Belt:
                    return new Belt(itemData, id, skillBase);
                case ItemType.Necklace:
                    return new Necklace(itemData, id, skillBase);
                case ItemType.Ring:
                    return new Ring(itemData, id, skillBase);
                case ItemType.Helm:
                    return new Helm(itemData, id, skillBase);
                case ItemType.Set:
                    return new SetItem(itemData, id, skillBase);
                case ItemType.Food:
                    return new Food(itemData, id, skillBase);
                case ItemType.Shoes:
                    return new Shoes(itemData, id, skillBase);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public abstract string ToItemInfo();

        public static Sprite GetSprite(ItemBase item = null)
        {
            int? id;
            if (item is ItemUsable itemUsable)
            {
 
                id = itemUsable.Data.resourceId;
            }
            else
            {
                id = item?.Data.id;
            }

            if (Equals(id, null) || Equals(id, 0))
            {
                id = DefaultId;
            }

            return GetSprite(id.Value);
        }

        public static Sprite GetSprite(int id)
        {
            var equips = Tables.instance.ItemEquipment;
            var items = Tables.instance.Item;
            string path = string.Empty;
            if (equips.ContainsKey(id))
            {
                path = EquipmentPath;
            }
            else if (items.ContainsKey(id))
            {
                path = ItemPath;
            }
            else
            {
                path = ItemPath;
                id = DefaultId;
            }

            return Resources.Load<Sprite>(string.Format(path, id));
        }
    }
}
