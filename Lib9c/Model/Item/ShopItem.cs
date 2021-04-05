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
            if (serialized.ContainsKey(LegacySellerAgentAddressKey))
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
            }
            else
            {
                SellerAgentAddress = serialized[SellerAgentAddressKey].ToAddress();
                SellerAvatarAddress = serialized[SellerAvatarAddressKey].ToAddress();
                ProductId = serialized[ProductIdKey].ToGuid();
                Price = serialized[PriceKey].ToFungibleAssetValue();
                ItemUsable = serialized.ContainsKey(ItemUsableKey)
                    ? (ItemUsable) ItemFactory.Deserialize((Dictionary) serialized[ItemUsableKey])
                    : null;
                Costume = serialized.ContainsKey(CostumeKey)
                    ? (Costume) ItemFactory.Deserialize((Dictionary) serialized[CostumeKey])
                    : null;
            }
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
