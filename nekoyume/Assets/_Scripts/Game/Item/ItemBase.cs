using System;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemBase
    {
        private const string ItemPath = "images/item/{0}";
        private const string EquipmentPath = "images/equipment/{0}";
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

        public Data.Table.Item Data { get; private set; }
        public bool reserved = false;

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

        public static ItemBase ItemFactory(Data.Table.Item itemData)
        {
            var type = itemData.cls.ToEnumItemType();
            switch (type)
            {
                case ItemType.Material:
                    return new Material(itemData);
                case ItemType.Weapon:
                    return new Weapon(itemData);
                case ItemType.RangedWeapon:
                    return new RangedWeapon(itemData);
                case ItemType.Armor:
                    return new Armor(itemData);
                case ItemType.Belt:
                    return new Belt(itemData);
                case ItemType.Necklace:
                    return new Necklace(itemData);
                case ItemType.Ring:
                    return new Ring(itemData);
                case ItemType.Helm:
                    return new Helm(itemData);
                case ItemType.Set:
                    return new SetItem(itemData);
                case ItemType.Food:
                    return new Food(itemData);
                case ItemType.Shoes:
                    return new Shoes(itemData);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public abstract string ToItemInfo();

        public static Sprite GetSprite(ItemBase item = null)
        {
            var path = item is ItemUsable ? EquipmentPath : ItemPath;
            return Resources.Load<Sprite>(string.Format(path, item?.Data.id)) ??
                   Resources.Load<Sprite>(string.Format(path, DefaultId));
        }
    }
}