using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nekoyume.UI.Model
{
    public class DccAvatars
    {
        [JsonProperty("avatars")]
        public Dictionary<string, int> Avatars { get; set; }

        public static DccAvatars FromJson(string json)
        {
            return JsonConvert.DeserializeObject<DccAvatars>(json, Converter.Settings);
        }

        private static class Converter
        {
            public static readonly JsonSerializerSettings Settings = new()
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                Converters =
                {
                    new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
                }
            };
        }
    }
}
