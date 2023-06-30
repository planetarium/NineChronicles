using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class PriceSchema
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}
