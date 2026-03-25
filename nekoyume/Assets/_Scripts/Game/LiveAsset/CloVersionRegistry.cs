using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nekoyume.Game.LiveAsset
{
    public class CloVersionRegistry
    {
        [JsonPropertyName("default")]
        public string Default { get; set; } = "internal";

        [JsonPropertyName("currentMainnetVersions")]
        public List<string> CurrentMainnetVersions { get; set; }

        [JsonPropertyName("stagingVersion")]
        public string StagingVersion { get; set; }

        [JsonPropertyName("internalVersion")]
        public string InternalVersion { get; set; }

        public string GetEnvironment(string appVersion)
        {
            if (!string.IsNullOrEmpty(InternalVersion) && appVersion == InternalVersion)
                return "internal";

            if (!string.IsNullOrEmpty(StagingVersion) && appVersion == StagingVersion)
                return "mainnet";

            if (CurrentMainnetVersions != null && CurrentMainnetVersions.Contains(appVersion))
                return "mainnet";

            return Default ?? "internal";
        }
    }
}
