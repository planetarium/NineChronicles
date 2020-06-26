using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class RedeemCodeStateTest
    {
        private RedeemCodeListSheet _sheet;
        [OneTimeSetUp]
        public void Init()
        {
            var tableSheets = TableSheetsHelper.MakeTableSheets();
            _sheet = tableSheets.RedeemCodeListSheet;
        }

        [Test]
        public void Serialize()
        {
            var state = new RedeemCodeState(_sheet);
            var serialized = (Dictionary) state.Serialize();
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "address"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "map"));
            var deserialized = new RedeemCodeState(serialized);
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
        public void RedeemThrowInvalidRedeemCodeException()
        {
            var state = new RedeemCodeState(_sheet);
            var key = new PrivateKey().PublicKey;
            Assert.IsFalse(state.Map.ContainsKey(key));
            Assert.Throws<InvalidRedeemCodeException>(() => state.Redeem(new PrivateKey().PublicKey, new Address()));
        }

        [Test]
        public void Redeem()
        {
            var state = new RedeemCodeState(_sheet);
            var key = state.Map.Keys.First();
            var result = state.Redeem(key, new Address());
            Assert.AreEqual(1, result);
        }

        [Test]
        public void RedeemThrowDuplicateRedeemException()
        {
            var state = new RedeemCodeState(_sheet);
            var key = state.Map.Keys.First();
            var address = new Address();
            state.Redeem(key, address);

            var exc = Assert.Throws<DuplicateRedeemException>(() => state.Redeem(key, new Address()));
            Assert.AreEqual(exc.Message, $"Code already used by {address}");
        }
    }
}
