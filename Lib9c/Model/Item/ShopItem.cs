using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class ShopItem
    {
        public const string ExpiredBlockIndexKey = "ebi";
        protected static readonly Codec Codec = new Codec();

        public readonly Address SellerAgentAddress;
        public readonly Address SellerAvatarAddress;
        public readonly Guid ProductId;
        public readonly FungibleAssetValue Price;
        public readonly ItemUsable ItemUsable;
        public readonly Costume Costume;
        public readonly Material Material;
        public readonly int MaterialCount;
        private long _expiredBlockIndex;

        public long ExpiredBlockIndex
        {
            get => _expiredBlockIndex;
            private set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException($"{nameof(ExpiredBlockIndex)} must be 0 or more, but {value}");
                }

                _expiredBlockIndex = value;
            }
        }

        public ShopItem(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            ItemUsable itemUsable) : this(sellerAgentAddress, sellerAvatarAddress, productId, price, 0, itemUsable)
        {
        }

        public ShopItem(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            Costume costume) : this(sellerAgentAddress, sellerAvatarAddress, productId, price, 0, costume)
        {
        }

        public ShopItem(
            Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            long expiredBlockIndex,
            INonFungibleItem nonFungibleItem
        )
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            ProductId = productId;
            Price = price;
            ExpiredBlockIndex = expiredBlockIndex;
            switch (nonFungibleItem)
            {
                case ItemUsable itemUsable:
                    ItemUsable = itemUsable;
                    break;
                case Costume costume:
                    Costume = costume;
                    break;
                default:
                    throw new ArgumentException(
                        $"{nameof(nonFungibleItem)} should be able to cast as ItemUsable or Costume.");
            }
        }

        public ShopItem(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            long expiredBlockIndex,
            ITradableItem tradableItem,
            int count)
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            ProductId = productId;
            Price = price;
            ExpiredBlockIndex = expiredBlockIndex;

            switch (tradableItem)
            {
                case Material material:
                    Material = material;
                    MaterialCount = count;
                    break;
                default:
                    throw new ArgumentException(
                        $"{nameof(tradableItem)} should be able to cast as Material.");
            }
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
            Material = serialized.ContainsKey("material")
                ? (Material) ItemFactory.Deserialize((Dictionary) serialized["material"])
                : null;
            MaterialCount = serialized.ContainsKey("materialCount")
                ? serialized["materialCount"].ToInteger()
                : default;
            if (serialized.ContainsKey(ExpiredBlockIndexKey))
            {
                ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
            }
        }

        protected ShopItem(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("serialized", Codec.Encode(Serialize()));
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

            if (Material != null)
            {
                innerDictionary.Add((Text) "material", Material.Serialize());
            }

            if (MaterialCount != 0)
            {
                innerDictionary.Add((Text) "materialCount", MaterialCount.Serialize());
            }

            if (ExpiredBlockIndex != 0)
            {
                innerDictionary.Add((Text) ExpiredBlockIndexKey, ExpiredBlockIndex.Serialize());
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
