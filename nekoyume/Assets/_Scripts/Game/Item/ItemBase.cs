using System;
using Nekoyume.Data;
using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class ItemBase
    {
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

        public Data.Table.Item Data { get; }
        
        public ItemBase(Data.Table.Item data)
        {
            Data = data;
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

        public abstract string ToItemInfo();

        public virtual Sprite GetIconSprite()
        {
            return SpriteHelper.GetItemIcon(Data.id);
        }
        
        public virtual Sprite GetBackgroundSprite()
        {
            return SpriteHelper.GetItemBackground(Data.grade);
        }
    }
}
