using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class ReceiptDetailSchema
    {
        [JsonPropertyName("store")]
        public Store Store { get; set; }

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("status")]
        public ReceiptStatus Status { get; set; }

        [JsonPropertyName("tx_id")]
        public string TxId { get; set; }

        [JsonPropertyName("tx_status")]
        public TxStatus TxStatus { get; set; }
    }
}
