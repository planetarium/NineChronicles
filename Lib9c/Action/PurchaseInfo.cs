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
    public class PurchaseInfo : IComparable<PurchaseInfo>, IComparable, IPurchaseInfo
    {
        public static bool operator >(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) > 0;

        public static bool operator <(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) < 0;

        public static bool operator >=(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) >= 0;

        public static bool operator <=(PurchaseInfo left, PurchaseInfo right) => left.CompareTo(right) <= 0;

        public static bool operator ==(PurchaseInfo left, PurchaseInfo right) => left.Equals(right);
        public static bool operator !=(PurchaseInfo left, PurchaseInfo right) => !(left == right);

        protected bool Equals(PurchaseInfo other)
        {
            return OrderId.Equals(other.OrderId) &&
                   TradableId.Equals(other.TradableId) &&
                   SellerAgentAddress.Equals(other.SellerAgentAddress) &&
                   SellerAvatarAddress.Equals(other.SellerAvatarAddress) &&
                   ItemSubType == other.ItemSubType &&
                   Price.Equals(other.Price);
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
                var hashCode = OrderId.GetHashCode();
                hashCode = (hashCode * 397) ^ TradableId.GetHashCode();
                hashCode = (hashCode * 397) ^ SellerAgentAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ SellerAvatarAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) ItemSubType;
                hashCode = (hashCode * 397) ^ Price.GetHashCode();
                return hashCode;
            }
        }

        public readonly Guid OrderId;
        Guid? IPurchaseInfo.OrderId => OrderId;
        public readonly Guid TradableId;
        public Address SellerAgentAddress { get; }
        public Address SellerAvatarAddress { get; }
        public ItemSubType ItemSubType { get; }
        public FungibleAssetValue Price { get; }

        public PurchaseInfo(
            Guid orderId,
            Guid tradableId,
            Address agentAddress,
            Address avatarAddress,
            ItemSubType type,
            FungibleAssetValue itemPrice
        )
        {
            OrderId = orderId;
            SellerAgentAddress = agentAddress;
            SellerAvatarAddress = avatarAddress;
            ItemSubType = type;
            TradableId = tradableId;
            Price = itemPrice;
        }

        public PurchaseInfo(Bencodex.Types.Dictionary serialized)
        {
            TradableId = serialized[TradableIdKey].ToGuid();
            OrderId = serialized[OrderIdKey].ToGuid();
            SellerAvatarAddress = serialized[SellerAvatarAddressKey].ToAddress();
            SellerAgentAddress = serialized[SellerAgentAddressKey].ToAddress();
            ItemSubType = serialized[ItemSubTypeKey].ToEnum<ItemSubType>();
            Price = serialized[PriceKey].ToFungibleAssetValue();
        }

        public IValue Serialize()
        {
            var dictionary = new Dictionary<IKey, IValue>
            {
                [(Text) OrderIdKey] = OrderId.Serialize(),
                [(Text) SellerAvatarAddressKey] = SellerAvatarAddress.Serialize(),
                [(Text) SellerAgentAddressKey] = SellerAgentAddress.Serialize(),
                [(Text) ItemSubTypeKey] = ItemSubType.Serialize(),
                [(Text) PriceKey] = Price.Serialize(),
                [(Text) TradableIdKey] = TradableId.Serialize(),
            };
            return new Bencodex.Types.Dictionary(dictionary);
        }

        public int CompareTo(PurchaseInfo other)
        {
            return OrderId.CompareTo(other.OrderId);
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
