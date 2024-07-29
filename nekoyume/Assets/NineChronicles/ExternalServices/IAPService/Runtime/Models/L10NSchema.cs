using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class L10NSchema
    {
        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("product")]
        public string Product { get; set; }
    }
}
