using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class OrderBase
    {
        public readonly Guid OrderId;
        public readonly Guid TradableId;
        private long _startedBlockIndex;
        private long _expiredBlockIndex;

        public long StartedBlockIndex
        {
            get => _startedBlockIndex;
            private set
            {
                if (value < 0 || _expiredBlockIndex > 0 && value > _expiredBlockIndex)
                {
                    throw new ArgumentOutOfRangeException($"{nameof(StartedBlockIndex)} must be 0 or more and less than {_expiredBlockIndex}, but {value}");
                }

                _startedBlockIndex = value;
            }
        }

        public long ExpiredBlockIndex
        {
            get => _expiredBlockIndex;
            private set
            {
                if (value < 0 || _startedBlockIndex > 0 && value < _startedBlockIndex)
                {
                    throw new ArgumentOutOfRangeException($"{nameof(ExpiredBlockIndex)} must be 0 or {_startedBlockIndex} more, but {value}");
                }

                _expiredBlockIndex = value;
            }
        }

        public OrderBase(Guid orderId, Guid tradableId, long startedBlockIndex, long expiredBlockIndex)
        {
            OrderId = orderId;
            TradableId = tradableId;
            StartedBlockIndex = startedBlockIndex;
            ExpiredBlockIndex = expiredBlockIndex;
        }

        public OrderBase(Dictionary serialized)
        {
            OrderId = serialized[OrderIdKey].ToGuid();
            TradableId = serialized[TradableIdKey].ToGuid();
            StartedBlockIndex = serialized[StartedBlockIndexKey].ToLong();
            ExpiredBlockIndex = serialized[ExpiredBlockIndexKey].ToLong();
        }


        public virtual IValue Serialize()
        {
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) OrderIdKey] = OrderId.Serialize(),
                [(Text) TradableIdKey] = TradableId.Serialize(),
                [(Text) StartedBlockIndexKey] = StartedBlockIndex.Serialize(),
                [(Text) ExpiredBlockIndexKey] = ExpiredBlockIndex.Serialize(),
            });
        }

        protected bool Equals(OrderBase other)
        {
            return OrderId.Equals(other.OrderId) &&
                   TradableId.Equals(other.TradableId) &&
                   _startedBlockIndex == other._startedBlockIndex &&
                   _expiredBlockIndex == other._expiredBlockIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderBase) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OrderId.GetHashCode();
                hashCode = (hashCode * 397) ^ TradableId.GetHashCode();
                hashCode = (hashCode * 397) ^ _startedBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ _expiredBlockIndex.GetHashCode();
                return hashCode;
            }
        }
    }
}
