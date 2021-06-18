namespace Lib9c.Tests.Model.Order
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Nekoyume;
    using Xunit;

    public class OrderReceiptTest
    {
        [Fact]
        public void Serialize()
        {
            Guid orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            var receipt = new OrderReceipt(orderId, Addresses.Admin, Addresses.Blacksmith, 10);
            Dictionary serialized = (Dictionary)receipt.Serialize();
            var deserialized = new OrderReceipt(serialized);

            Assert.Equal(orderId, deserialized.OrderId);
            Assert.Equal(Addresses.Admin, deserialized.BuyerAgentAddress);
            Assert.Equal(Addresses.Blacksmith, deserialized.BuyerAvatarAddress);
            Assert.Equal(10, deserialized.TransferredBlockIndex);
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            Guid orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            var receipt = new OrderReceipt(orderId, Addresses.Admin, Addresses.Blacksmith, 10);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, receipt);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (OrderReceipt)formatter.Deserialize(ms);

            Assert.Equal(receipt.Serialize(), deserialized.Serialize());
        }
    }
}
