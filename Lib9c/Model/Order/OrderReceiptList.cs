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
    public class OrderReceiptList
    {
        public static Address DeriveAddress(Address avatarAddress)
        {
            return avatarAddress.Derive(nameof(OrderReceiptList));
        }

        public readonly Address Address;

        private List<OrderReceipt> _receiptList = new List<OrderReceipt>();

        public IReadOnlyList<OrderReceipt> ReceiptList => _receiptList;

        public OrderReceiptList(Address address)
        {
            Address = address;
        }

        public OrderReceiptList(Dictionary serialized)
        {
            Address = serialized[AddressKey].ToAddress();
            _receiptList = serialized[OrderReceiptListKey]
                .ToList(m => new OrderReceipt((Dictionary)m))
                .OrderBy(o => o.OrderId)
                .ToList();
        }

        public IValue Serialize()
        {
            var innerDict = new Dictionary<IKey, IValue>
            {
                [(Text) AddressKey] = Address.Serialize(),
                [(Text) OrderReceiptListKey] = new List(_receiptList.Select(m => m.Serialize())),
            };

            return new Dictionary(innerDict);
        }

        public void Add(Order order, long blockIndex)
        {
            OrderReceipt receipt = order.Receipt();
            if (_receiptList.Contains(receipt))
            {
                throw new DuplicateOrderIdException($"{order.OrderId} already exist.");
            }
            _receiptList.Add(receipt);

            _receiptList = _receiptList
                .Where(r => r.ExpiredBlockIndex >= blockIndex)
                .OrderBy(r => r.StartedBlockIndex)
                .ToList();
        }

        protected bool Equals(OrderReceiptList other)
        {
            return Address.Equals(other.Address) && _receiptList.SequenceEqual(other._receiptList);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OrderReceiptList) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Address.GetHashCode() * 397) ^ (_receiptList != null ? _receiptList.GetHashCode() : 0);
            }
        }
    }
}
