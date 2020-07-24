namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Nekoyume.Model.State;
    using Xunit;

    public class WeeklyArenaStateTest
    {
        [Theory]
        [InlineData(1, "44971f56cDDe257b355B7faD618DbD67085e8BB8")]
        [InlineData(2, "866F0C71E0F701cCCCEBAfA17daAbdaB9ee702C1")]
        public void DeriveAddress(int index, string expected)
        {
            var state = new WeeklyArenaState(index);
            Assert.Equal(new Address(expected), state.address);
        }

        [Fact]
        public void Serialize()
        {
            var address = default(Address);
            var state = new WeeklyArenaState(address);

            var serialized = (Dictionary)state.Serialize();
            var deserialized = new WeeklyArenaState(serialized);

            Assert.Equal(state.address, deserialized.address);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            var address = default(Address);
            var state = new WeeklyArenaState(address);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, state);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (WeeklyArenaState)formatter.Deserialize(ms);
            Assert.Equal(state.address, deserialized.address);
        }
    }
}
