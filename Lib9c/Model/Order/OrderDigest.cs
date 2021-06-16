using System;
using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class OrderDigest : OrderBase
    {
        // Client filter data
        public readonly FungibleAssetValue Price;
        public readonly int CombatPoint;
        public readonly int Level;
        public readonly int ItemId;

        public OrderDigest(long startedBlockIndex,
            long expiredBlockIndex,
            Guid orderId,
            Guid tradableId,
            FungibleAssetValue price,
            int combatPoint,
            int level,
            int itemId
        ) : base(orderId, tradableId, startedBlockIndex, expiredBlockIndex)
        {
            Price = price;
            CombatPoint = combatPoint;
            Level = level;
            ItemId = itemId;
        }

        public OrderDigest(Dictionary serialized) : base(serialized)
        {
            Price = serialized[PriceKey].ToFungibleAssetValue();
            CombatPoint = serialized[CombatPointKey].ToInteger();
            Level = serialized[LevelKey].ToInteger();
            ItemId = serialized[ItemIdKey].ToInteger();
        }

        public override IValue Serialize()
        {
            var innerDict = ((Dictionary) base.Serialize())
                .SetItem(PriceKey, Price.Serialize())
                .SetItem(CombatPointKey, CombatPoint.Serialize())
                .SetItem(LevelKey, Level.Serialize())
                .SetItem(ItemIdKey, ItemId.Serialize());

            return new Dictionary(innerDict);
        }

        protected bool Equals(OrderDigest other)
        {
            return base.Equals(other) &&
                   Price.Equals(other.Price) &&
                   CombatPoint == other.CombatPoint &&
                   Level == other.Level &&
                   ItemId == other.ItemId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderDigest) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StartedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ ExpiredBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ OrderId.GetHashCode();
                hashCode = (hashCode * 397) ^ TradableId.GetHashCode();
                hashCode = (hashCode * 397) ^ Price.GetHashCode();
                hashCode = (hashCode * 397) ^ CombatPoint;
                hashCode = (hashCode * 397) ^ Level;
                hashCode = (hashCode * 397) ^ ItemId;
                return hashCode;
            }
        }
    }
}
