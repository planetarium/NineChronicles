using System.Text.Json.Serialization;
using Libplanet.Crypto;

namespace Nekoyume.GraphQL.GraphTypes
{
    public class PledgeGraphType
    {
        public class InnerType
        {
            [JsonPropertyName("patronAddress")]
            public Address? PatronAddress;

            [JsonPropertyName("approved")]
            public bool Approved;

            [JsonPropertyName("mead")]
            public int Mead;

            public override string ToString()
            {
                return $"PatronAddress: {PatronAddress}, Approved: {Approved}, Mead: {Mead}";
            }
        }

        [JsonPropertyName("pledge")]
        public InnerType? Pledge;

        public override string ToString()
        {
            var inner = Pledge is null ? "null" : Pledge.ToString();
            return $"Pledge: {{ {inner} }}";
        }
    }
}
