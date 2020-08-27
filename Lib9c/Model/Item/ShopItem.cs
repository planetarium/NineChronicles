using System;
using System.Collections.Generic;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class ShopItem
    {
        public readonly Address SellerAgentAddress;
        public readonly Address SellerAvatarAddress;
        public readonly Guid ProductId;
        public readonly ItemUsable ItemUsable;
        public readonly FungibleAssetValue Price;

        public ShopItem(
            Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            ItemUsable itemUsable,
            FungibleAssetValue price)
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            ProductId = productId;
            ItemUsable = itemUsable;
            Price = price;
        }

        public ShopItem(Dictionary serialized)
        {
            SellerAgentAddress = serialized["sellerAgentAddress"].ToAddress();
            SellerAvatarAddress = serialized["sellerAvatarAddress"].ToAddress();
            ProductId = serialized["productId"].ToGuid();
            ItemUsable = (ItemUsable)ItemFactory.Deserialize(
                (Dictionary)serialized["itemUsable"]
            );
            Price = serialized["price"].ToFungibleAssetValue();
        }

        public IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"sellerAgentAddress"] = SellerAgentAddress.Serialize(),
                [(Text)"sellerAvatarAddress"] = SellerAvatarAddress.Serialize(),
                [(Text)"productId"] = ProductId.Serialize(),
                [(Text)"itemUsable"] = ItemUsable.Serialize(),
                [(Text)"price"] = Price.Serialize(),
            });

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
    }
}
