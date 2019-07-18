using System;
using Libplanet;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class ShopItem
    {
        public Address sellerAvatarAddress;
        public Guid productId;
        public ItemUsable itemUsable;
        public decimal price;
    }
}
