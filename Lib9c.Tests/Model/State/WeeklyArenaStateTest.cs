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
