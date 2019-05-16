using System;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class ShopItem
    {
        public string productId;
        public byte[] owner;
        public ItemBase item;
        public int count;
        public decimal price;
    }
}
