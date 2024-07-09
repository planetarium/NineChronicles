using System.Text.Json.Serialization;

namespace Nekoyume.GraphQL.GraphTypes
{
    public class StateQueryGraphType<T>
    {
        [JsonPropertyName("stateQuery")]
        public T StateQuery;
    }
}
