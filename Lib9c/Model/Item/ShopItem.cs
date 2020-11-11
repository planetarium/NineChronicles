using System;
using System.Collections.Generic;
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
        public readonly FungibleAssetValue Price;
        public readonly ItemUsable ItemUsable;
        public readonly Costume Costume;

        public ShopItem(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            ItemUsable itemUsable)
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            ProductId = productId;
            Price = price;
            ItemUsable = itemUsable;
            Costume = null;
        }

        public ShopItem(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            Costume costume)
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            ProductId = productId;
            Price = price;
            ItemUsable = null;
            Costume = costume;
        }

        public ShopItem(Dictionary serialized)
        {
            SellerAgentAddress = serialized["sellerAgentAddress"].ToAddress();
            SellerAvatarAddress = serialized["sellerAvatarAddress"].ToAddress();
            ProductId = serialized["productId"].ToGuid();
            Price = serialized["price"].ToFungibleAssetValue();
            ItemUsable = serialized.ContainsKey("itemUsable")
                ? (ItemUsable) ItemFactory.Deserialize((Dictionary) serialized["itemUsable"])
                : null;
            Costume = serialized.ContainsKey("costume")
                ? (Costume) ItemFactory.Deserialize((Dictionary) serialized["costume"])
                : null;
        }

        public IValue Serialize()
        {
            var innerDictionary = new Dictionary<IKey, IValue>
            {
                [(Text) "sellerAgentAddress"] = SellerAgentAddress.Serialize(),
                [(Text) "sellerAvatarAddress"] = SellerAvatarAddress.Serialize(),
                [(Text) "productId"] = ProductId.Serialize(),
                [(Text) "price"] = Price.Serialize(),
            };

            if (ItemUsable != null)
            {
                innerDictionary.Add((Text) "itemUsable", ItemUsable.Serialize());
            }

            if (Costume != null)
            {
                innerDictionary.Add((Text) "costume", Costume.Serialize());
            }

            return new Dictionary(innerDictionary);
        }


        public IValue SerializeBackup1() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "sellerAgentAddress"] = SellerAgentAddress.Serialize(),
                [(Text) "sellerAvatarAddress"] = SellerAvatarAddress.Serialize(),
                [(Text) "productId"] = ProductId.Serialize(),
                [(Text) "itemUsable"] = ItemUsable.Serialize(),
                [(Text) "price"] = Price.Serialize(),
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
            return Equals((ShopItem) obj);
        }

        public override int GetHashCode()
        {
            return ProductId.GetHashCode();
        }
    }
}