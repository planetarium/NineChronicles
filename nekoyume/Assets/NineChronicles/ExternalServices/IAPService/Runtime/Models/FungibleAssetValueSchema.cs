using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class FungibleAssetValueSchema
    {
        [JsonPropertyName("ticker")]
        public Currency Ticker { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }
}
