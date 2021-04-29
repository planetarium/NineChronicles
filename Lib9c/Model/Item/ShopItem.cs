using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

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
            SellerAgentAddress = serialized[LegacySellerAgentAddressKey].ToAddress();
            SellerAvatarAddress = serialized[LegacySellerAvatarAddressKey].ToAddress();
            ProductId = serialized[LegacyProductIdKey].ToGuid();
            Price = serialized[LegacyPriceKey].ToFungibleAssetValue();
            ItemUsable = serialized.ContainsKey(LegacyItemUsableKey)
                ? (ItemUsable) ItemFactory.Deserialize((Dictionary) serialized[LegacyItemUsableKey])
                : null;
            Costume = serialized.ContainsKey(LegacyCostumeKey)
                ? (Costume) ItemFactory.Deserialize((Dictionary) serialized[LegacyCostumeKey])
                : null;
            Material = serialized.ContainsKey(MaterialKey)
                ? (Material) ItemFactory.Deserialize((Dictionary) serialized[MaterialKey])
                : null;
            MaterialCount = serialized.ContainsKey(MaterialCountKey)
                ? serialized[MaterialCountKey].ToInteger()
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
                [(Text) LegacySellerAgentAddressKey] = SellerAgentAddress.Serialize(),
                [(Text) LegacySellerAvatarAddressKey] = SellerAvatarAddress.Serialize(),
                [(Text) LegacyProductIdKey] = ProductId.Serialize(),
                [(Text) LegacyPriceKey] = Price.Serialize(),
            };

            if (ItemUsable != null)
            {
                innerDictionary.Add((Text) LegacyItemUsableKey, ItemUsable.Serialize());
            }

            if (Costume != null)
            {
                innerDictionary.Add((Text) LegacyCostumeKey, Costume.Serialize());
            }

            if (Material != null)
            {
                innerDictionary.Add((Text) MaterialKey, Material.Serialize());
            }

            if (MaterialCount != 0)
            {
                innerDictionary.Add((Text) MaterialCountKey, MaterialCount.Serialize());
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
                [(Text) LegacySellerAgentAddressKey] = SellerAgentAddress.Serialize(),
                [(Text) LegacySellerAvatarAddressKey] = SellerAvatarAddress.Serialize(),
                [(Text) LegacyProductIdKey] = ProductId.Serialize(),
                [(Text) LegacyItemUsableKey] = ItemUsable.Serialize(),
                [(Text) LegacyPriceKey] = Price.Serialize(),
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
