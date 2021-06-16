using System;
using Bencodex.Types;

namespace Lib9c.Model.Order
{
    [Serializable]
    public class OrderReceipt : OrderBase
    {
        public OrderReceipt(Order order) : base(order.OrderId, order.TradableId, order.StartedBlockIndex, order.ExpiredBlockIndex)
        {
        }

        public OrderReceipt(Dictionary serialized) : base(serialized)
        {
        }
    }
}
