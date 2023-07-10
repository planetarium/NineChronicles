using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class FungibleItemSchema
    {
        [JsonPropertyName("sheet_item_id")]
        public int SheetItemId { get; set; }

        [JsonPropertyName("fungible_item_id")]
        public string FungibleItemId { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }
    }
}
