using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class ProductSchema
    {
        [JsonPropertyName("google_sku")]
        public string GoogleSku { get; set; }

        [JsonPropertyName("product_type")]
        public ProductType ProductType { get; set; }

        [JsonPropertyName("daily_limit")]
        public int? DailyLimit { get; set; }

        [JsonPropertyName("weekly_limit")]
        public int? WeeklyLimit { get; set; }

        [JsonPropertyName("purchase_count")]
        public int PurchaseCount { get; set; }

        [JsonPropertyName("display_order")]
        public int DisplayOrder { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("buyable")]
        public bool Buyable { get; set; }

        [JsonPropertyName("fav_list")]
        public FungibleAssetValueSchema[] FavList { get; set; }

        [JsonPropertyName("fungible_item_list")]
        public FungibleItemSchema[] FungibleItemList { get; set; }

        [JsonPropertyName("price_list")]
        public PriceSchema[] PriceList { get; set; }
    }
}
