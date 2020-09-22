namespace Lib9c.Tests.Action
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class InvalidSignatureExceptionTest
    {
        [Fact]
        public void Serialize()
        {
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            var pending = new PendingActivationState(
                new byte[] { 0x00 },
                new PrivateKey().PublicKey
            );
            var exc = new InvalidSignatureException(
                pending,
                new byte[] { 0x01 }
            );

            formatter.Serialize(ms, exc);
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized =
                (InvalidSignatureException)formatter.Deserialize(ms);

            Assert.Equal(exc.Pending.Serialize(), deserialized.Pending.Serialize());
            Assert.Equal(exc.Signature, deserialized.Signature);
        }
    }
}
