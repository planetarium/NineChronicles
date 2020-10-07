namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Xunit;

    public class AdminStateTest
    {
        [Fact]
        public void Serialize()
        {
            var adminStateAddress = new PrivateKey().ToAddress();
            var validUntil = 100;
            var adminState = new AdminState(adminStateAddress, validUntil);

            var serialized = adminState.Serialize();
            var deserialized = new AdminState((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(adminStateAddress, deserialized.AdminAddress);
            Assert.Equal(validUntil, deserialized.ValidUntil);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            var adminStateAddress = new PrivateKey().ToAddress();
            var validUntil = 100;
            var adminState = new AdminState(adminStateAddress, validUntil);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, adminState);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (AdminState)formatter.Deserialize(ms);

            Assert.Equal(adminStateAddress, deserialized.AdminAddress);
            Assert.Equal(validUntil, deserialized.ValidUntil);
        }
    }
}
