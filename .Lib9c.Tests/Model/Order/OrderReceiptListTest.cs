namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Nekoyume.Action;
    using Xunit;

    public class OrderReceiptListTest
    {
        private readonly Order _order;

        public OrderReceiptListTest()
        {
            var orderId = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var tradableId = new Guid("936DA01F-9ABD-4d9d-80C7-02AF85C822A8");
            _order = OrderFactory.Create(default, default, orderId, default, tradableId, 1, default, 1);
        }

        [Fact]
        public void Add()
        {
            var address = OrderReceiptList.DeriveAddress(default);
            var orderReceiptList = new OrderReceiptList(address);
            orderReceiptList.Add(_order, 0);

            Assert.Single(orderReceiptList.ReceiptList);
            OrderReceipt orderReceipt = orderReceiptList.ReceiptList.First();
            Assert.Equal(_order.OrderId, orderReceipt.OrderId);
            Assert.Equal(_order.TradableId, orderReceipt.TradableId);
            Assert.Equal(_order.StartedBlockIndex, orderReceipt.StartedBlockIndex);
            Assert.Equal(_order.ExpiredBlockIndex, orderReceipt.ExpiredBlockIndex);

            Assert.Throws<DuplicateOrderIdException>(() => orderReceiptList.Add(_order, 0));

            Order order = OrderFactory.Create(default, default, Guid.NewGuid(), default, default, 10, default, 1);
            orderReceiptList.Add(order, _order.ExpiredBlockIndex + 1);
            Assert.Single(orderReceiptList.ReceiptList);
            Assert.Equal(order.OrderId, orderReceiptList.ReceiptList.First().OrderId);
        }

        [Fact]
        public void Serialize()
        {
            var address = OrderReceiptList.DeriveAddress(default);
            var orderReceiptList = new OrderReceiptList(address);
            orderReceiptList.Add(_order, 0);

            Dictionary serialized = (Dictionary)orderReceiptList.Serialize();
            Assert.Equal(orderReceiptList, new OrderReceiptList(serialized));
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            var address = OrderReceiptList.DeriveAddress(default);
            var orderReceiptList = new OrderReceiptList(address);
            orderReceiptList.Add(_order, 0);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, orderReceiptList);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (OrderReceiptList)formatter.Deserialize(ms);

            Assert.Equal(orderReceiptList.Serialize(), deserialized.Serialize());
        }
    }
}
