#nullable enable

using System.Linq;
using System.Text.Json.Serialization;
using Libplanet.Crypto;

namespace Nekoyume.GraphQL.GraphTypes
{
    public class AgentGraphType
    {
        public class InnerType
        {
            [JsonPropertyName("address")]
            public Address Address;

            [JsonPropertyName("avatarAddress")]
            public AvatarGraphType[] AvatarStates;

            public override string ToString()
            {
                return $"Address: {Address}, AvatarStates({AvatarStates.Length}):" +
                       $" [{string.Join(", ", AvatarStates.Select(e => e.ToString()))}]";
            }
        }

        [JsonPropertyName("agent")]
        public InnerType? Agent;

        public override string ToString()
        {
            var inner = Agent is null ? "null" : Agent.ToString();
            return $"Agent: {{ {inner} }}";
        }
    }
}
