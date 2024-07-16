using System.Text.Json.Serialization;

namespace Nekoyume.GraphQL.GraphTypes
{
    public class AgentAndPledgeGraphType
    {
        [JsonPropertyName("agent")]
        public AgentGraphType.InnerType? Agent;

        [JsonPropertyName("pledge")]
        public PledgeGraphType.InnerType Pledge;

        public override string ToString()
        {
            var agent = Agent is null ? "null" : Agent.ToString();
            var pledge = Pledge is null ? "null" : Pledge.ToString();
            return $"Agent: {{ {agent} }}, Pledge: {{ {pledge} }}";
        }
    }
}
