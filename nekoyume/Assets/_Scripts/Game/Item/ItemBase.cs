using System;
using Nekoyume.Data.Table;
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

        public static ItemBase ItemFactory(Data.Table.Item itemData, SkillBase skillBase = null, string id = null)
        {
            var type = itemData.cls.ToEnumItemType();
            switch (type)
            {
                case ItemType.Material:
                    return new Material(itemData);
                case ItemType.Weapon:
                    return new Weapon(itemData, skillBase, id);
                case ItemType.RangedWeapon:
                    return new RangedWeapon(itemData, skillBase, id);
                case ItemType.Armor:
                    return new Armor(itemData, skillBase, id);
                case ItemType.Belt:
                    return new Belt(itemData, skillBase, id);
                case ItemType.Necklace:
                    return new Necklace(itemData, skillBase, id);
                case ItemType.Ring:
                    return new Ring(itemData, skillBase, id);
                case ItemType.Helm:
                    return new Helm(itemData, skillBase, id);
                case ItemType.Set:
                    return new SetItem(itemData, skillBase, id);
                case ItemType.Food:
                    return new Food(itemData, skillBase, id);
                case ItemType.Shoes:
                    return new Shoes(itemData, skillBase, id);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public abstract string ToItemInfo();

        public static Sprite GetSprite(ItemBase item = null)
        {
            string path;
            int? id;
            var casting = item as ItemUsable;
            if (!ReferenceEquals(casting, null))
            {
                path = EquipmentPath;
                id = casting.Data.resourceId;
            }
            else
            {
                path = ItemPath;
                id = item?.Data.id;
            }

            if (Equals(id, null) || Equals(id, 0))
            {
                id = DefaultId;
            }

            return Resources.Load<Sprite>(string.Format(path, id));
        }
    }
}
