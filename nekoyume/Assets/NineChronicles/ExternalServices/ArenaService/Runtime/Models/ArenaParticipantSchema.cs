using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.ArenaService.Runtime.Models
{
    public class ArenaParticipantSchema
    {
        [JsonPropertyName("avatar_addr")]
        public string AvartarAddr { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("cp")]
        public int CP { get; set; }

        [JsonPropertyName("costume_id")]
        public int CostumeId { get; set; }

        [JsonPropertyName("title_id")]
        public int? TitleId { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("rank")]
        public int Rank { get; set; }
    }
}
