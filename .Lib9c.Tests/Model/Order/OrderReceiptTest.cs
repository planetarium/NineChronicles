namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Libplanet.Assets;
    using Xunit;

    public class OrderReceiptTest
    {
        private readonly Order _order;

        public OrderReceiptTest()
        {
            var currency = new Currency("NCG", 2, minter: null);
            var orderId = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var tradableId = new Guid("936DA01F-9ABD-4d9d-80C7-02AF85C822A8");
            _order = OrderFactory.Create(
                default,
                default,
                orderId,
                currency * 0,
                tradableId,
                0,
                default,
                1
            );
        }

        [Fact]
        public void Serialize()
        {
            OrderReceipt orderReceipt = _order.Receipt();
            Dictionary serialized = (Dictionary)orderReceipt.Serialize();
            Assert.Equal(orderReceipt, new OrderReceipt(serialized));
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            OrderReceipt orderReceipt = _order.Receipt();
            BinaryFormatter formatter = new BinaryFormatter();
            using MemoryStream ms = new MemoryStream();
            formatter.Serialize(ms, orderReceipt);
            ms.Seek(0, SeekOrigin.Begin);

            OrderReceipt deserialized = (OrderReceipt)formatter.Deserialize(ms);
            Assert.Equal(orderReceipt, deserialized);
        }
    }
}
