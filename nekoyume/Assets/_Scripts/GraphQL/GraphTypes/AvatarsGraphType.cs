#nullable enable

using System.Linq;
using System.Text.Json.Serialization;

namespace Nekoyume.GraphQL.GraphTypes
{
    public class AvatarsGraphType
    {
        [JsonPropertyName("avatars")]
        public AvatarGraphType?[] Avatars;

        public override string ToString()
        {
            return $"Avatars({Avatars.Length}):" +
                   $" [ {string.Join(", ", Avatars.Select(e => e is null ? "null" : $"{{ {e} }}"))} ]";
        }
    }
}
