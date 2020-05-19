using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class RedeemCodeTest
    {
        [Test]
        public void Serialize()
        {
            var redeemCode = new PrivateKey().PublicKey;
            var address = new Address();
            var action = new RedeemCode
            {
                code = redeemCode,
                avatarAddress = address
            };
            var serialized = (Dictionary) action.PlainValue;
            Assert.IsTrue(serialized.ContainsKey((Text) "code"));
            Assert.IsTrue(serialized.ContainsKey((Text) "avatarAddress"));
            Assert.AreEqual(redeemCode, serialized["code"].ToPublicKey());
            Assert.AreEqual(address, serialized["avatarAddress"].ToAddress());
        }
    }
}
