using Libplanet.Types.Assets;
using Nekoyume.State.Modifiers;
using NUnit.Framework;

namespace Tests.EditMode.State.Modifiers
{
    public class AgentNCGModifierTest
    {
        private Currency _currency;
        private FungibleAssetValue _ncgFav;

        [SetUp]
        public void SetUp()
        {
            _currency = Currency.Legacy("NCG", 2, null);
            _ncgFav = new FungibleAssetValue(_currency);
        }

        [Test]
        public void AddTest()
        {
            Assert.True(_ncgFav.Equals(new FungibleAssetValue(_currency)));
            var fav = new FungibleAssetValue(_currency, 100, 0);
            var modifier = new AgentNCGModifier(fav);
            var beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav));

            var fav2 = new FungibleAssetValue(_currency, -100, 0);
            modifier.Add(new AgentNCGModifier(fav2));
            beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav + fav2));

            var fav3 = new FungibleAssetValue(_currency, -100, 0);
            modifier.Add(new AgentNCGModifier(fav3));
            beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav + fav2 + fav3));
        }

        [Test]
        public void RemoveTest()
        {
            Assert.True(_ncgFav.Equals(new FungibleAssetValue(_currency)));
            var fav = new FungibleAssetValue(_currency, 100, 0);
            var modifier = new AgentNCGModifier(fav);
            var beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav));

            var fav2 = new FungibleAssetValue(_currency, -100, 0);
            modifier.Remove(new AgentNCGModifier(fav2));
            beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav - fav2));

            var fav3 = new FungibleAssetValue(_currency, -100, 0);
            modifier.Remove(new AgentNCGModifier(fav3));
            beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav - fav2 - fav3));
        }

        [Test]
        public void ModifyTest()
        {
            Assert.True(_ncgFav.Equals(new FungibleAssetValue(_currency)));
            var fav = new FungibleAssetValue(_currency, 100, 0);
            var modifier = new AgentNCGModifier(fav);
            var beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav));

            fav = new FungibleAssetValue(_currency, -100, 0);
            modifier = new AgentNCGModifier(fav);
            beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav));

            fav = new FungibleAssetValue(_currency, -100, 0);
            modifier = new AgentNCGModifier(fav);
            beforeFav = _ncgFav;
            _ncgFav = modifier.Modify(_ncgFav);
            Assert.True(_ncgFav.Equals(beforeFav + fav));
        }
    }
}
