namespace Lib9c.Tests.Model
{
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Xunit;

    public class ActivationKeyTest
    {
        [Fact]
        public void Create()
        {
            var privateKey = new PrivateKey(ByteUtil.ParseHex("ac84ad2eb0bc62c63e8c4e4f22f7c19d283b36e60fdc4eed182d4d7a7bb4c716"));
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            (ActivationKey ak, PendingActivationState pending) = ActivationKey.Create(privateKey, nonce);

            Assert.Equal(privateKey, ak.PrivateKey);
            Assert.Equal(pending.address, ak.PendingAddress);
            Assert.Equal(privateKey.PublicKey, pending.PublicKey);
            Assert.Equal(nonce, pending.Nonce);
        }

        [Fact]
        public void Encode()
        {
            var privateKey = new PrivateKey(ByteUtil.ParseHex("ac84ad2eb0bc62c63e8c4e4f22f7c19d283b36e60fdc4eed182d4d7a7bb4c716"));
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            (ActivationKey ak, var _) = ActivationKey.Create(privateKey, nonce);

            Assert.Equal("ac84ad2eb0bc62c63e8c4e4f22f7c19d283b36e60fdc4eed182d4d7a7bb4c716/46c4365d645647791768b9a9e6e4a9376b15c643", ak.Encode());
        }

        [Fact]
        public void Decode()
        {
            string encoded = "ac84ad2eb0bc62c63e8c4e4f22f7c19d283b36e60fdc4eed182d4d7a7bb4c716/46c4365d645647791768b9a9e6e4a9376b15c643";
            ActivationKey ak = ActivationKey.Decode(encoded);

            Assert.Equal(new Address("46c4365d645647791768b9a9e6e4a9376b15c643"), ak.PendingAddress);
            Assert.Equal(new PrivateKey(ByteUtil.ParseHex("ac84ad2eb0bc62c63e8c4e4f22f7c19d283b36e60fdc4eed182d4d7a7bb4c716")), ak.PrivateKey);
        }
    }
}
