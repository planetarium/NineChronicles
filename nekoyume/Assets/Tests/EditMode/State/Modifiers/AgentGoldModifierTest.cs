using Libplanet;
using Libplanet.Assets;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;
using NUnit.Framework;

namespace Tests.EditMode.State.Modifiers
{
    public class AgentGoldModifierTest
    {
        private Currency _currency;
        private GoldBalanceState _goldBalanceState;

        [SetUp]
        public void SetUp()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldBalanceState = new GoldBalanceState(
                new PrivateKey().ToAddress(),
                new FungibleAssetValue(_currency));
        }

        [TearDown]
        public void TearDown()
        {
            _goldBalanceState = null;
            _currency = default;
        }

        [Test]
        public void AddTest()
        {
            Assert.True(_goldBalanceState.Gold.Equals(new FungibleAssetValue(_currency)));
            var fav = new FungibleAssetValue(_currency, 100, 0);
            var modifier = new AgentGoldModifier(fav);
            var beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav));

            var fav2 = new FungibleAssetValue(_currency, -100, 0);
            modifier.Add(new AgentGoldModifier(fav2));
            beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav + fav2));

            var fav3 = new FungibleAssetValue(_currency, -100, 0);
            modifier.Add(new AgentGoldModifier(fav3));
            beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav + fav2 + fav3));
        }

        [Test]
        public void RemoveTest()
        {
            Assert.True(_goldBalanceState.Gold.Equals(new FungibleAssetValue(_currency)));
            var fav = new FungibleAssetValue(_currency, 100, 0);
            var modifier = new AgentGoldModifier(fav);
            var beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav));

            var fav2 = new FungibleAssetValue(_currency, -100, 0);
            modifier.Remove(new AgentGoldModifier(fav2));
            beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav - fav2));

            var fav3 = new FungibleAssetValue(_currency, -100, 0);
            modifier.Remove(new AgentGoldModifier(fav3));
            beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav - fav2 - fav3));
        }

        [Test]
        public void ModifyTest()
        {
            Assert.True(_goldBalanceState.Gold.Equals(new FungibleAssetValue(_currency)));
            var fav = new FungibleAssetValue(_currency, 100, 0);
            var modifier = new AgentGoldModifier(fav);
            var beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav));

            fav = new FungibleAssetValue(_currency, -100, 0);
            modifier = new AgentGoldModifier(fav);
            beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav));

            fav = new FungibleAssetValue(_currency, -100, 0);
            modifier = new AgentGoldModifier(fav);
            beforeFav = _goldBalanceState.Gold;
            _goldBalanceState = modifier.Modify(_goldBalanceState);
            Assert.True(_goldBalanceState.Gold.Equals(beforeFav + fav));
        }
    }
}