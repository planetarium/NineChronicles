namespace Lib9c.Tests.Action
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Nekoyume.Action;
    using Xunit;

    public class PendingActivationDoesNotExistsExceptionTest
    {
        [Fact]
        public void Serialize()
        {
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            var exc = new PendingActivationDoesNotExistsException(
                new Address(new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x01,
                })
            );

            formatter.Serialize(ms, exc);
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized =
                (PendingActivationDoesNotExistsException)formatter.Deserialize(ms);

            Assert.Equal(exc.PendingAddress, deserialized.PendingAddress);
        }
    }
}
