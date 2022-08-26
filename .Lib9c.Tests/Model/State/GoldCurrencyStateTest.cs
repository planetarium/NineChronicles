namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume.Model.State;
    using Xunit;

    public class GoldCurrencyStateTest
    {
        [Fact]
        public void Serialize()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, default(Address));
#pragma warning restore CS0618
            var state = new GoldCurrencyState(currency);
            var serialized = (Dictionary)state.Serialize();
            GoldCurrencyState deserialized = new GoldCurrencyState(serialized);

            Assert.Equal(currency.Hash, deserialized.Currency.Hash);
        }

        [Fact]
        public void SerializeWithDotnetAPI()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, default(Address));
#pragma warning restore CS0618
            var state = new GoldCurrencyState(currency);
            var formatter = new BinaryFormatter();

            using var ms = new MemoryStream();
            formatter.Serialize(ms, state);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (GoldCurrencyState)formatter.Deserialize(ms);

            Assert.Equal(currency.Hash, deserialized.Currency.Hash);
        }
    }
}
