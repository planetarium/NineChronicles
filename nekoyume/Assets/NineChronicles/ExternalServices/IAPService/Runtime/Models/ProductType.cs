using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    [JsonConverter(typeof(ProductTypeJsonConverter))]
    public enum ProductType
    {
        SINGLE,
        PACKAGE
    }

    public class ProductTypeJsonConverter : JsonConverter<ProductType>
    {
        public override ProductType Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return value switch
            {
                "SINGLE" => ProductType.SINGLE,
                "PACKAGE" => ProductType.PACKAGE,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            ProductType value,
            JsonSerializerOptions options)
        {
            switch (value)
            {
                case ProductType.SINGLE:
                    writer.WriteStringValue("SINGLE");
                    break;
                case ProductType.PACKAGE:
                    writer.WriteStringValue("PACKAGE");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}
