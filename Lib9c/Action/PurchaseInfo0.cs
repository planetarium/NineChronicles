using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    public class PurchaseInfo0 : IComparable<PurchaseInfo0>, IComparable, IPurchaseInfo
    {
        public static bool operator >(PurchaseInfo0 left, PurchaseInfo0 right) => left.CompareTo(right) > 0;

        public static bool operator <(PurchaseInfo0 left, PurchaseInfo0 right) => left.CompareTo(right) < 0;

        public static bool operator >=(PurchaseInfo0 left, PurchaseInfo0 right) => left.CompareTo(right) >= 0;

        public static bool operator <=(PurchaseInfo0 left, PurchaseInfo0 right) => left.CompareTo(right) <= 0;

        public static bool operator ==(PurchaseInfo0 left, PurchaseInfo0 right) => left.Equals(right);
        public static bool operator !=(PurchaseInfo0 left, PurchaseInfo0 right) => !(left == right);

        protected bool Equals(PurchaseInfo0 other)
        {
            return productId.Equals(other.productId) && sellerAgentAddress.Equals(other.sellerAgentAddress) && sellerAvatarAddress.Equals(other.sellerAvatarAddress) && itemSubType == other.itemSubType && price.Equals(other.price);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PurchaseInfo0) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = productId.GetHashCode();
                hashCode = (hashCode * 397) ^ sellerAgentAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ sellerAvatarAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) itemSubType;
                hashCode = (hashCode * 397) ^ price.GetHashCode();
                return hashCode;
            }
        }

        public readonly Guid productId;
        public readonly Address sellerAgentAddress;
        public readonly Address sellerAvatarAddress;
        public readonly ItemSubType itemSubType;
        public readonly FungibleAssetValue price;

        public PurchaseInfo0(Guid id, Address agentAddress, Address avatarAddress, ItemSubType type, FungibleAssetValue itemPrice = default)
        {
            productId = id;
            sellerAgentAddress = agentAddress;
            sellerAvatarAddress = avatarAddress;
            itemSubType = type;
            if (!itemPrice.Equals(default))
            {
                price = itemPrice;
            }
        }

        public PurchaseInfo0(Bencodex.Types.Dictionary serialized)
        {
            productId = serialized[ProductIdKey].ToGuid();
            sellerAvatarAddress = serialized[SellerAvatarAddressKey].ToAddress();
            sellerAgentAddress = serialized[SellerAgentAddressKey].ToAddress();
            itemSubType = serialized[ItemSubTypeKey].ToEnum<ItemSubType>();
            if (serialized.ContainsKey(PriceKey))
            {
                price = serialized[PriceKey].ToFungibleAssetValue();
            }
        }

        Guid? IPurchaseInfo.OrderId => null;
        Address IPurchaseInfo.SellerAgentAddress => sellerAgentAddress;
        Address IPurchaseInfo.SellerAvatarAddress => sellerAvatarAddress;
        FungibleAssetValue IPurchaseInfo.Price => price;
        ItemSubType IPurchaseInfo.ItemSubType => itemSubType;

        public IValue Serialize()
        {
            var dictionary = new Dictionary<IKey, IValue>
            {
                [(Text) ProductIdKey] = productId.Serialize(),
                [(Text) SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
                [(Text) SellerAgentAddressKey] = sellerAgentAddress.Serialize(),
                [(Text) ItemSubTypeKey] = itemSubType.Serialize(),
            };
            if (!price.Equals(default))
            {
                dictionary[(Text) PriceKey] = price.Serialize();
            }
            return new Bencodex.Types.Dictionary(dictionary);
        }

        public int CompareTo(PurchaseInfo0 other)
        {
            return productId.CompareTo(other.productId);
        }

        public int CompareTo(object obj)
        {
            if (obj is PurchaseInfo0 other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException(nameof(obj));
        }
    }
}
