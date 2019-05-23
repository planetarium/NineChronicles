using System;
using Libplanet;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class ShopItem
    {
        public Address sellerAgentAddress;
        public Guid productId;
        public ItemBase item;
        public int count;
        public decimal price;
    }
}
