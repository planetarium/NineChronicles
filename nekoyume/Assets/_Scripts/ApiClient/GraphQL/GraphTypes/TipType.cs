#nullable enable

using System.Text.Json.Serialization;
using Libplanet.Crypto;

namespace Nekoyume.GraphQL.GraphTypes
{
    public class TipType
    {
        public class InnerType
        {
            [JsonPropertyName("id")]
            public string? id;

            [JsonPropertyName("hash")]
            public string? hash;

            [JsonPropertyName("index")]
            public int? index;

            [JsonPropertyName("miner")]
            public Address? miner;

            public override string ToString()
            {
                return $"Id: {id}, Hash: {hash}, Index: {index}, Miner: {miner}";
            }
        }

        [JsonPropertyName("tip")]
        public InnerType? Tip;

        public override string ToString()
        {
            var inner = Tip is null ? "null" : Tip.ToString();
            return $"Tip: {{ {inner} }}";
        }
    }
}
