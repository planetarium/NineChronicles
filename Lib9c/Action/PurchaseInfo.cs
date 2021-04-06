using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    public class PurchaseInfo : IComparable<PurchaseInfo>, IComparable
    {
        public static bool operator >(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) > 0;

        public static bool operator <(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) < 0;

        public static bool operator >=(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) >= 0;

        public static bool operator <=(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) <= 0;

        public static bool operator ==(PurchaseInfo left, PurchaseInfo right) => left.Equals(right);
        public static bool operator !=(PurchaseInfo left, PurchaseInfo right) => !(left == right);

        protected bool Equals(PurchaseInfo other)
        {
            return productId.Equals(other.productId) && sellerAgentAddress.Equals(other.sellerAgentAddress) && sellerAvatarAddress.Equals(other.sellerAvatarAddress);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PurchaseInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = productId.GetHashCode();
                hashCode = (hashCode * 397) ^ sellerAgentAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ sellerAvatarAddress.GetHashCode();
                return hashCode;
            }
        }

        public readonly Guid productId;
        public readonly Address sellerAgentAddress;
        public readonly Address sellerAvatarAddress;
        public readonly ItemSubType itemSubType;

        public PurchaseInfo(Guid id, Address agentAddress, Address avatarAddress, ItemSubType type)
        {
            productId = id;
            sellerAgentAddress = agentAddress;
            sellerAvatarAddress = avatarAddress;
            itemSubType = type;
        }

        public PurchaseInfo(Bencodex.Types.Dictionary serialized)
        {
            productId = serialized[ProductIdKey].ToGuid();
            sellerAvatarAddress = serialized[SellerAvatarAddressKey].ToAddress();
            sellerAgentAddress = serialized[SellerAgentAddressKey].ToAddress();
            itemSubType = serialized[ItemSubTypeKey].ToEnum<ItemSubType>();
        }

        public IValue Serialize() =>
#pragma warning disable LAA1002
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) ProductIdKey] = productId.Serialize(),
                [(Text) SellerAvatarAddressKey] = sellerAvatarAddress.Serialize(),
                [(Text) SellerAgentAddressKey] = sellerAgentAddress.Serialize(),
                [(Text) ItemSubTypeKey] = itemSubType.Serialize(),
            });
#pragma warning restore LAA1002
        public int CompareTo(PurchaseInfo other)
        {
            return productId.CompareTo(other.productId);
        }

        public int CompareTo(object obj)
        {
            if (obj is PurchaseInfo other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException(nameof(obj));
        }
    }
}
