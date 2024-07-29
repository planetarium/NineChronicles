using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NineChronicles.ExternalServices.IAPService.Runtime.Common;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    [JsonConverter(typeof(StoreTypeConverter))]
    public enum Store
    {
        Test = 0,
        Apple = 1,
        Google = 2,
        AppleTest = 91,
        GoogleTest = 92
    }

    public class StoreTypeConverter : JsonConverter<Store>
    {
        public override Store Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => (Store)reader.GetInt32(),
                JsonTokenType.String => Enum.Parse<Store>(reader.GetString()),
                _ => throw JsonExceptionFactory.TokenType(
                    new[] { JsonTokenType.Number, JsonTokenType.String },
                    reader.TokenType)
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            Store value,
            JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }
}
