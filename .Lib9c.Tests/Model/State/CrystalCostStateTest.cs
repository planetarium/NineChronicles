namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Xunit;

    public class CrystalCostStateTest
    {
        [Fact]
        public void Serialize()
        {
            var crystal = 100 * CrystalCalculator.CRYSTAL;
            var address = new PrivateKey().ToAddress();
            var state = new CrystalCostState(address, crystal);
            state.Count++;
            var serialized = state.Serialize();
            var deserialized = new CrystalCostState(address, (List)serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.CRYSTAL, deserialized.CRYSTAL);
            Assert.Equal(state.Count, deserialized.Count);
        }
    }
}
