using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class PurchaseProcessResultSchema
    {
        [JsonPropertyName("store")]
        public Store Store { get; set; }

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("status")]
        public ReceiptStatus Status { get; set; }
    }
}
