using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class ItemBase
    {
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
            ItemBase,
            Weapon,
            RangedWeapon,
            Armor,
            Belt,
            Necklace,
            Ring,
            Helm,
        }

        public static ItemBase ItemFactory(Data.Table.Item itemData)
        {
            var type = (ItemType) Enum.Parse(typeof(ItemType), itemData.Cls);
            switch (type)
            {
                case ItemType.ItemBase:
                    return new ItemBase(itemData);
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public virtual string ToItemInfo()
        {
            return "";
        }
    }
}
