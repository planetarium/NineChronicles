using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class OrderDigestListState
    {
        public static Address DeriveAddress(Address avatarAddress)
        {
            return avatarAddress.Derive(nameof(OrderDigestListState));
        }

        public readonly Address Address;

        private List<OrderDigest> _orderDigestList = new List<OrderDigest>();

        public IReadOnlyList<OrderDigest> OrderDigestList => _orderDigestList;

        public OrderDigestListState(Address address)
        {
            Address = address;
        }

        public OrderDigestListState(Dictionary serialized)
        {
            Address = serialized[AddressKey].ToAddress();
            _orderDigestList = serialized[OrderReceiptListKey]
                .ToList(m => new OrderDigest((Dictionary)m))
                .OrderBy(o => o.OrderId)
                .ToList();
        }

        public IValue Serialize()
        {
            var innerDict = new Dictionary<IKey, IValue>
            {
                [(Text) AddressKey] = Address.Serialize(),
                [(Text) OrderReceiptListKey] = new List(_orderDigestList.Select(m => m.Serialize())),
            };

            return new Dictionary(innerDict);
        }

        public void Add(OrderDigest orderDigest)
        {
            if (_orderDigestList.Contains(orderDigest))
            {
                throw new DuplicateOrderIdException($"{orderDigest.OrderId} already exist.");
            }
            _orderDigestList.Add(orderDigest);
        }

        public void Remove(Guid orderId)
        {
            var target = _orderDigestList.SingleOrDefault(o => o.OrderId.Equals(orderId));
            if (target is null)
            {
                throw new OrderIdDoesNotExistException($"Can't find {nameof(OrderDigest)}: {orderId}");
            }

            _orderDigestList.Remove(target);
        }

        protected bool Equals(OrderDigestListState other)
        {
            return Address.Equals(other.Address) && _orderDigestList.SequenceEqual(other._orderDigestList);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderDigestListState) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Address.GetHashCode() * 397) ^ (_orderDigestList != null ? _orderDigestList.GetHashCode() : 0);
            }
        }
    }
}
