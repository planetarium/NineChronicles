using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    public class CategorySchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("l10n_key")]
        public string L10n_Key { get; set; }

        [JsonPropertyName("product_list")]
        public ProductSchema[] ProductList { get; set; }
    }
}
