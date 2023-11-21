using System.Text.Json.Serialization;

namespace Nekoyume.GraphQL.GraphTypes
{
    public class AvatarGraphType
    {
        [JsonPropertyName("address")]
        public string Address;

        [JsonPropertyName("name")]
        public string Name;

        [JsonPropertyName("level")]
        public int Level;

        public override string ToString()
        {
            return $"Avatar: {{ Address({Address}), Name({Name}), Level({Level}) }}";
        }
    }
}
