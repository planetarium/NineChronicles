#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nekoyume.Multiplanetary
{
    public class PlanetIdJsonConverter : JsonConverter<PlanetId>
    {
        public override PlanetId Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return new PlanetId(reader.GetString()!);
        }

        public override void Write(
            Utf8JsonWriter writer,
            PlanetId value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class NullablePlanetIdJsonConverter : JsonConverter<PlanetId?>
    {
        public override PlanetId? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return value is null
                ? null
                : new PlanetId(value);
        }

        public override void Write(
            Utf8JsonWriter writer,
            PlanetId? value,
            JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.ToString());
        }
    }
}
