using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    [JsonConverter(typeof(CurrencyJsonConverter))]
    public enum Currency
    {
        NCG,
        CRYSTAL,
        GARAGE,
    }

    public class CurrencyJsonConverter : JsonConverter<Currency>
    {
        public override Currency Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return value switch
            {
                "NCG" => Currency.NCG,
                "CRYSTAL" => Currency.CRYSTAL,
                "GARAGE"=> Currency.GARAGE,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            Currency value,
            JsonSerializerOptions options)
        {
            switch (value)
            {
                case Currency.NCG:
                    writer.WriteStringValue("NCG");
                    break;
                case Currency.CRYSTAL:
                    writer.WriteStringValue("CRYSTAL");
                    break;
                case Currency.GARAGE:
                    writer.WriteStringValue("GARAGE");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}
