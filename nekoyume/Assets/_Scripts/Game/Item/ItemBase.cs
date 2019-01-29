using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class ItemBase
    {
        public Data.Table.Item Data { get; private set; }

        public ItemBase(Data.Table.Item data)
        {
            Data = data;
        }

        private enum ItemType
        {
            ItemBase,
            Weapon
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
