namespace Lib9c.Tests.Action
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Nekoyume.Action;
    using Xunit;

    public class InvalidTransferSiginerExceptionTest
    {
        [Fact]
        public void Serialize()
        {
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            var exc = new InvalidTransferSignerException(
                new Address(new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x01,
                }),
                new Address(new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x02,
                }),
                new Address(new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x03,
                })
            );

            formatter.Serialize(ms, exc);
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized =
                (InvalidTransferSignerException)formatter.Deserialize(ms);

            Assert.Equal(exc.TxSigner, deserialized.TxSigner);
            Assert.Equal(exc.Recipient, deserialized.Recipient);
            Assert.Equal(exc.Sender, deserialized.Sender);
        }
    }
}
