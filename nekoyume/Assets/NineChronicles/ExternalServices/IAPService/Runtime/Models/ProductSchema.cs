using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class ProductSchema
    {
        public class Fav
        {
            [JsonPropertyName("ticker")]
            public Currency Ticker { get; set; }

            [JsonPropertyName("amount")]
            public int Amount { get; set; }
        }

        public class FungibleItem
        {
            [JsonPropertyName("item_id")]
            public int ItemId { get; set; }

            [JsonPropertyName("amount")]
            public int Amount { get; set; }
        }

        [JsonPropertyName("google_sku")]
        public string GoogleSku { get; set; }

        [JsonPropertyName("product_type")]
        public ProductType ProductType { get; set; }

        [JsonPropertyName("daily_limit")]
        public int? DailyLimit { get; set; }

        [JsonPropertyName("weekly_limit")]
        public int? WeeklyLimit { get; set; }

        [JsonPropertyName("display_order")]
        public int DisplayOrder { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("fav_list")]
        public Fav[] FavList { get; set; }

        [JsonPropertyName("item_list")]
        public FungibleItem[] ItemList { get; set; }
    }
}
