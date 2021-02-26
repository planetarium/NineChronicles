namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Xunit;

    public class AgentStateTest
    {
        [Fact]
        public void Serialize()
        {
            var agentStateAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentStateAddress);

            var serialized = agentState.Serialize();
            var deserialized = new AgentState((Bencodex.Types.Dictionary)serialized);

            Assert.Equal(agentStateAddress, deserialized.address);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            var agentStateAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentStateAddress);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, agentState);
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (AgentState)formatter.Deserialize(ms);

            Assert.Equal(agentStateAddress, deserialized.address);
        }
    }
}
