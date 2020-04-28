using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.State;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class PromotionCodeStateTest
    {
        [Test]
        public void Serialize()
        {
            var state = new PromotionCodeState();
            var serialized = (Dictionary) state.Serialize();
            Assert.IsTrue(serialized.ContainsKey((Text) "address"));
            Assert.IsTrue(serialized.ContainsKey((Text) "map"));
            var deserialized = new PromotionCodeState(serialized);
            Assert.AreEqual(state.address, deserialized.address);
            foreach (var pair in state.Map)
            {
                var expected = pair.Value;
                var actual = deserialized.Map[pair.Key];
                Assert.AreEqual(expected.RewardId, actual.RewardId);
                Assert.AreEqual(expected.UserAddress, actual.UserAddress);
            }
        }

        [Test]
        public void RedeemThrowKeyNotFoundException()
        {
            var state = new PromotionCodeState();
            var key = new Address();
            Assert.IsFalse(state.Map.ContainsKey(key));
            Assert.Throws<KeyNotFoundException>(() => state.Redeem(new Address(), new Address()));
        }

        [Test]
        public void Redeem()
        {
            var state = new PromotionCodeState();
            var key = state.Map.Keys.First();
            var result = state.Redeem(key, new Address());
            Assert.AreEqual(400000, result);
        }

        [Test]
        public void RedeemThrowInvalidOperationException()
        {
            var state = new PromotionCodeState();
            var key = state.Map.Keys.First();
            var address = new Address();
            state.Redeem(key, address);

            var exc = Assert.Throws<InvalidOperationException>(() => state.Redeem(key, new Address()));
            Assert.AreEqual(exc.Message, $"Code already used by {address}");
        }
    }
}
