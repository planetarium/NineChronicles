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
        public readonly ITradableFungibleItem TradableFungibleItem;
        public readonly int TradableFungibleItemCount;
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
            ITradableItem tradableItem) : this(sellerAgentAddress, sellerAvatarAddress, productId, price, 0, tradableItem)
        {
        }

        public ShopItem(Address sellerAgentAddress,
            Address sellerAvatarAddress,
            Guid productId,
            FungibleAssetValue price,
            long expiredBlockIndex,
            ITradableItem tradableItem,
            int tradableItemCount = 1)
        {
            SellerAgentAddress = sellerAgentAddress;
            SellerAvatarAddress = sellerAvatarAddress;
            ProductId = productId;
            Price = price;
            ExpiredBlockIndex = expiredBlockIndex;

            switch (tradableItem)
            {
                case ItemUsable itemUsable:
                    ItemUsable = itemUsable;
                    break;
                case Costume costume:
                    Costume = costume;
                    break;
                case ITradableFungibleItem tradableFungibleItem:
                    TradableFungibleItem = tradableFungibleItem;
                    TradableFungibleItemCount = tradableItemCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(tradableItem)} should be able to case as ItemUsable or Costume or Material");
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
            TradableFungibleItem = serialized.ContainsKey(TradableFungibleItemKey)
                ? (ITradableFungibleItem) ItemFactory.Deserialize((Dictionary) serialized[TradableFungibleItemKey])
                : null;
            TradableFungibleItemCount = serialized.ContainsKey(TradableFungibleItemCountKey)
                ? serialized[TradableFungibleItemCountKey].ToInteger()
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

            if (TradableFungibleItem != null)
            {
                innerDictionary.Add((Text) TradableFungibleItemKey, TradableFungibleItem.Serialize());
            }

            if (TradableFungibleItemCount != 0)
            {
                innerDictionary.Add((Text) TradableFungibleItemCountKey, TradableFungibleItemCount.Serialize());
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
