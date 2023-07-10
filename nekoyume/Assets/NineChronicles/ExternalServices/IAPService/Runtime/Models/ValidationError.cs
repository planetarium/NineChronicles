using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NineChronicles.ExternalServices.IAPService.Runtime.Common;

namespace NineChronicles.ExternalServices.IAPService.Runtime.Models
{
    [JsonConverter(typeof(ValidationErrorJsonConverter))]
    public class ValidationError
    {
        public const string LocPropName = "loc";
        public const string MsgPropName = "msg";
        public const string TypePropName = "type";

        [JsonPropertyName(LocPropName)]
        public string[] Loc { get; set; }

        [JsonPropertyName(MsgPropName)]
        public string Message { get; set; }

        [JsonPropertyName(TypePropName)]
        public string Type { get; set; }
    }

    public class ValidationErrorJsonConverter : JsonConverter<ValidationError>
    {
        public override ValidationError Read(
            ref Utf8JsonReader reader,
            System.Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject");
            }

            var result = new ValidationError();
            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw JsonExceptionFactory.PropName(reader.TokenType);
                }

                var propName = reader.GetString();
                switch (propName)
                {
                    case ValidationError.LocPropName:
                    {
                        reader.Read();
                        if (reader.TokenType != JsonTokenType.StartArray)
                        {
                            throw new JsonException("Expected StartArray");
                        }

                        var loc = new List<string>();
                        reader.Read();
                        while (reader.TokenType != JsonTokenType.EndArray)
                        {
                            reader.Read();
                            loc.Add(reader.TokenType == JsonTokenType.Number
                                ? reader.GetInt32().ToString()
                                : reader.GetString());
                        }

                        result.Loc = loc.ToArray();
                        break;
                    }
                    case ValidationError.MsgPropName:
                    {
                        reader.Read();
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw JsonExceptionFactory.PropValue(
                                ValidationError.MsgPropName,
                                new[] { JsonTokenType.String },
                                reader.TokenType);
                        }

                        result.Message = reader.GetString();
                        break;
                    }
                    case ValidationError.TypePropName:
                    {
                        reader.Read();
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw JsonExceptionFactory.PropValue(
                                ValidationError.TypePropName,
                                new[] { JsonTokenType.String },
                                reader.TokenType);
                        }

                        result.Type = reader.GetString();
                        break;
                    }
                    default:
                        throw new JsonException($"Unknown property: {propName}");
                }
            }

            return result;
        }

        public override void Write(
            Utf8JsonWriter writer,
            ValidationError value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(ValidationError.LocPropName);
            writer.WriteStartArray();
            foreach (var loc in value.Loc)
            {
                if (int.TryParse(loc, out var locInt))
                {
                    writer.WriteNumberValue(locInt);
                    continue;
                }

                writer.WriteStringValue(loc);
            }

            writer.WriteEndArray();
            writer.WritePropertyName(ValidationError.MsgPropName);
            writer.WriteStringValue(value.Message);
            writer.WritePropertyName(ValidationError.TypePropName);
            writer.WriteStringValue(value.Type);
            writer.WriteEndObject();
        }
    }
}
