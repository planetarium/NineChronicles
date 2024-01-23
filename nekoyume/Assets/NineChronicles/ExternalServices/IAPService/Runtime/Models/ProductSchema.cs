using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class ProductSchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("google_sku")]
        public string GoogleSku { get; set; }

        [JsonPropertyName("apple_sku")]
        public string AppleSku { get; set; }

        public string Sku {
            get {
#if UNITY_ANDROID
                return GoogleSku;
#elif UNITY_IOS
                return AppleSku;
#else
                return GoogleSku;
#endif
            }
        }

        [JsonPropertyName("daily_limit")]
        public int? DailyLimit { get; set; }

        [JsonPropertyName("weekly_limit")]
        public int? WeeklyLimit { get; set; }

        [JsonPropertyName("account_limit")]
        public int? AccountLimit { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("buyable")]
        public bool Buyable { get; set; }

        [JsonPropertyName("purchase_count")]
        public int PurchaseCount { get; set; }

        [JsonPropertyName("size")]
        public string Size { get; set; }

        [JsonPropertyName("discount")]
        public int Discount { get; set; }

        [JsonPropertyName("l10n_key")]
        public string L10n_Key { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("bg_path")]
        public string BgPath { get; set; }

        [JsonPropertyName("popup_path_key")]
        public string PopupPathKey { get; set; }

        [JsonPropertyName("fav_list")]
        public FungibleAssetValueSchema[] FavList { get; set; }

        [JsonPropertyName("fungible_item_list")]
        public FungibleItemSchema[] FungibleItemList { get; set; }

        [JsonPropertyName("is_free")]
        public bool IsFree { get; set; }

        [JsonPropertyName("required_level")]
        public int? RequiredLevel { get; set; }
    } 
}
