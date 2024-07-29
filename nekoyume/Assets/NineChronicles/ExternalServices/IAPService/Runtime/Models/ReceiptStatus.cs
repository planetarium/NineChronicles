using System.Text.Json;
using System.Text.Json.Serialization;
using NineChronicles.ExternalServices.IAPService.Runtime.Common;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    [JsonConverter(typeof(ReceiptStatusConverter))]
    public enum ReceiptStatus
    {
        Init = 0,
        ValidationRequest = 1,
        Valid = 10,
        Invalid = 91,
        Unknown = 99
    }

    public class ReceiptStatusConverter : JsonConverter<ReceiptStatus>
    {
        public override ReceiptStatus Read(
            ref Utf8JsonReader reader,
            System.Type typeToConvert,
            JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => (ReceiptStatus)reader.GetInt32(),
                JsonTokenType.String => System.Enum.Parse<ReceiptStatus>(reader.GetString()),
                _ => throw JsonExceptionFactory.TokenType(
                    new[] { JsonTokenType.Number, JsonTokenType.String },
                    reader.TokenType)
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            ReceiptStatus value,
            JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }
}
