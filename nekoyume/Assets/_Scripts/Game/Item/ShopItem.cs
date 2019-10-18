using System;
using Libplanet;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class ShopItem
    {
        public readonly Address SellerAvatarAddress;
        public readonly Guid ProductId;
        public readonly ItemUsable ItemUsable;
        public readonly decimal Price;

        public ShopItem(Address sellerAvatarAddress, Guid productId, ItemUsable itemUsable, decimal price)
        {
            SellerAvatarAddress = sellerAvatarAddress;
            ProductId = productId;
            ItemUsable = itemUsable;
            Price = price;
        }

        protected bool Equals(ShopItem other)
        {
            return ProductId.Equals(other.ProductId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ShopItem) obj);
        }

        public override int GetHashCode()
        {
            return ProductId.GetHashCode();
        }
    }
}
