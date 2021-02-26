namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Xunit;

    public class PendingActivationStateTest
    {
        [Fact]
        public void Serialize()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var pubKey = new PublicKey(
                ByteUtil.ParseHex("02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1")
            );
            var state = new PendingActivationState(nonce, pubKey);

            var serialized = (Dictionary)state.Serialize();
            var deserialized = new PendingActivationState(serialized);

            Assert.Equal(
                new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7"),
                deserialized.address
            );
            Assert.Equal(pubKey, deserialized.PublicKey);
            Assert.Equal(nonce, deserialized.Nonce);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var pubKey = new PublicKey(
                ByteUtil.ParseHex("02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1")
            );
            var state = new PendingActivationState(nonce, pubKey);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, state);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (PendingActivationState)formatter.Deserialize(ms);

            Assert.Equal(
                new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7"),
                deserialized.address
            );
            Assert.Equal(pubKey, deserialized.PublicKey);
            Assert.Equal(nonce, deserialized.Nonce);
        }
    }
}
