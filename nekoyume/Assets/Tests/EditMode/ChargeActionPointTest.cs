using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class ChargeActionPointTest
    {
        [Test]
        public void Serialize()
        {
            var address = new Address();
            var action = new ChargeActionPoint
            {
                avatarAddress = address
            };
            var serialized = (Dictionary) action.PlainValue;
            Assert.IsTrue(serialized.ContainsKey((Text) "avatarAddress"));
            Assert.AreEqual(address, serialized["avatarAddress"].ToAddress());
            Assert.DoesNotThrow(() => ByteSerializer.Serialize(action));
        }
    }
}
