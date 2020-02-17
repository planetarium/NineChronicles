using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
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

        public ShopItem(Dictionary serialized)
        {
            SellerAvatarAddress = serialized["sellerAvatarAddress"].ToAddress();
            ProductId = serialized["productId"].ToGuid();
            ItemUsable = (ItemUsable)ItemFactory.Deserialize(
                (Dictionary)serialized["itemUsable"]
            );
            Price = serialized["price"].ToDecimal();
        }

        protected bool Equals(ShopItem other)
        {
            return ProductId.Equals(other.ProductId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ShopItem)obj);
        }

        public override int GetHashCode()
        {
            return ProductId.GetHashCode();
        }

        public IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"sellerAvatarAddress"] = SellerAvatarAddress.Serialize(),
                [(Text)"productId"] = ProductId.Serialize(),
                [(Text)"itemUsable"] = ItemUsable.Serialize(),
                [(Text)"price"] = Price.Serialize(),
            });
    }
}
