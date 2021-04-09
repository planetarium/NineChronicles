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
        protected static readonly Codec Codec = new Codec();
        
        public readonly Address SellerAgentAddress;
        public readonly Address SellerAvatarAddress;
        public readonly Guid ProductId;
        public readonly FungibleAssetValue Price;
        public readonly ItemUsable ItemUsable;
        public readonly Costume Costume;
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
                    Costume = null;
                    break;
                case Costume costume:
                    ItemUsable = null;
                    Costume = costume;
                    break;
            }
        }

        public ShopItem(Dictionary serialized)
        {
            bool useLegacy = serialized.ContainsKey(LegacySellerAgentAddressKey);
            string sellerAgentKey = useLegacy ? LegacySellerAgentAddressKey : SellerAgentAddressKey;
            string sellerAvatarKey = useLegacy ? LegacySellerAvatarAddressKey : SellerAvatarAddressKey;
            string productIdKey = useLegacy ? LegacyProductIdKey : ProductIdKey;
            string priceKey = useLegacy ? LegacyPriceKey : PriceKey;
            string itemUsableKey = useLegacy ? LegacyItemUsableKey : ItemUsableKey;
            string costumeKey = useLegacy ? LegacyCostumeKey : CostumeKey;
            SellerAgentAddress = serialized[sellerAgentKey].ToAddress();
            SellerAvatarAddress = serialized[sellerAvatarKey].ToAddress();
            ProductId = serialized[productIdKey].ToGuid();
            Price = serialized[priceKey].ToFungibleAssetValue();
            ItemUsable = serialized.ContainsKey(itemUsableKey)
                ? (ItemUsable) ItemFactory.Deserialize((Dictionary) serialized[itemUsableKey])
                : null;
            Costume = serialized.ContainsKey(costumeKey)
                ? (Costume) ItemFactory.Deserialize((Dictionary) serialized[costumeKey])
                : null;
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
                [(Text) SellerAgentAddressKey] = SellerAgentAddress.Serialize(),
                [(Text) SellerAvatarAddressKey] = SellerAvatarAddress.Serialize(),
                [(Text) ProductIdKey] = ProductId.Serialize(),
                [(Text) PriceKey] = Price.Serialize(),
            };

            if (ItemUsable != null)
            {
                innerDictionary.Add((Text) ItemUsableKey, ItemUsable.Serialize());
            }

            if (Costume != null)
            {
                innerDictionary.Add((Text) CostumeKey, Costume.Serialize());
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

        public IValue SerializeLegacy()
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
                innerDictionary.Add((Text) LegacyItemUsableKey, ItemUsable.SerializeLegacy());
            }

            if (Costume != null)
            {
                innerDictionary.Add((Text) LegacyCostumeKey, Costume.SerializeLegacy());
            }

            if (ExpiredBlockIndex != 0)
            {
                innerDictionary.Add((Text) ExpiredBlockIndexKey, ExpiredBlockIndex.Serialize());
            }

            return new Dictionary(innerDictionary);
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
            return Equals((ShopItem) obj);
        }

        public override int GetHashCode()
        {
            return ProductId.GetHashCode();
        }
    }
}
