using Libplanet.Crypto;
using Nekoyume.Model.State;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class StateExtensionsTest
    {
        [Test]
        public void SerializePublicKey()
        {
            var key = new PrivateKey().PublicKey;
            var serialized = key.Serialize();
            Assert.AreEqual(key, serialized.ToPublicKey());
        }
    }
}
