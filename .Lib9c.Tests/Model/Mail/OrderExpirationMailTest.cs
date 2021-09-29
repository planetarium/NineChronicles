namespace Lib9c.Tests.Model.Mail
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Nekoyume.Model.Mail;
    using Xunit;

    public class OrderExpirationMailTest
    {
        [Fact]
        public void Serialize()
        {
            var orderId = Guid.NewGuid();
            var mail = new OrderExpirationMail(1, Guid.NewGuid(), 2, orderId);
            var serialized = (Dictionary)mail.Serialize();
            var deserialized = (OrderExpirationMail)Mail.Deserialize(serialized);

            Assert.Equal(1, deserialized.blockIndex);
            Assert.Equal(2, deserialized.requiredBlockIndex);
            Assert.Equal(orderId, deserialized.OrderId);
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            var orderId = Guid.NewGuid();
            var mail = new OrderExpirationMail(1, Guid.NewGuid(), 2, orderId);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, mail);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (OrderExpirationMail)formatter.Deserialize(ms);

            Assert.Equal(mail.Serialize(), deserialized.Serialize());
        }
    }
}
