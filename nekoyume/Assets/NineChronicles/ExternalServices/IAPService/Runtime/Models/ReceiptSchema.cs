using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class ReceiptSchema
    {
        [JsonPropertyName("store")]
        public Store Store { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("agentAddress")]
        public string AgentAddress { get; set; }

        [JsonPropertyName("inventoryAddress")]
        public string InventoryAddress { get; set; }
    }
}
