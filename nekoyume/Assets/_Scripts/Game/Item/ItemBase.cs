using System;
using Nekoyume.Data;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemBase
    {
        public const int DefaultId = 101000;
        public const string ItemPath = "UI/Icons/Item/{0}";
        public const string EquipmentPath = "UI/Icons/Equipment/{0}";
        public const string GradeIconPath = "UI/Textures/item_bg_{0}";

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

        public static Sprite GetGradeIconSprite(int grade)
        {
            string path = string.Format(GradeIconPath, grade);
            return Resources.Load<Sprite>(path);
        }
    }
}
