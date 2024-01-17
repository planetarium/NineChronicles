using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nekoyume.Multiplanetary
{
    public class PlanetInfo
    {
        [JsonPropertyName("id")]
        public PlanetId ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("genesisHash")]
        public string GenesisHash { get; set; }

        [JsonPropertyName("genesisUri")]
        public string GenesisUri { get; set; }

        [JsonPropertyName("rpcEndpoints")]
        public RpcEndpoints RPCEndpoints { get; set; }

        [JsonPropertyName("guildIconBucket")]
        public string GuildIconBucket { get; set; }
    }

    public class RpcEndpoints
    {
        [JsonPropertyName("dp.gql")]
        public List<string> DataProviderGql { get; set; }

        [JsonPropertyName("9cscan.rest")]
        public List<string> _9CScanRest { get; set; }

        [JsonPropertyName("headless.gql")]
        public List<string> HeadlessGql { get; set; }

        [JsonPropertyName("headless.grpc")]
        public List<string> HeadlessGrpc { get; set; }

        [JsonPropertyName("market.rest")]
        public List<string> MarketRest { get; set; }

        [JsonPropertyName("world-boss.rest")]
        public List<string> WorldBossRest { get; set; }

        [JsonPropertyName("patrol-reward.gql")]
        public List<string> PatrolRewardGql { get; set; }

        [JsonPropertyName("guild.rest")]
        public List<string> GuildRest { get; set; } = new ();
    }
}
