using System.Text.Json.Serialization;

namespace Nekoyume.Game.LiveAsset
{
    public class CloVersionRegistry
    {
        [JsonPropertyName("default")]
        public string Default { get; set; } = "internal";

        [JsonPropertyName("currentMainnetVersion")]
        public string CurrentMainnetVersion { get; set; }

        [JsonPropertyName("stagingVersion")]
        public string StagingVersion { get; set; }

        public string GetEnvironment(string appVersion)
        {
            if (!string.IsNullOrEmpty(StagingVersion) && appVersion == StagingVersion)
                return "mainnet";

            if (!string.IsNullOrEmpty(CurrentMainnetVersion) &&
                System.Version.TryParse(appVersion, out var current) &&
                System.Version.TryParse(CurrentMainnetVersion, out var mainnet) &&
                current <= mainnet)
                return "mainnet";

            return Default ?? "internal";
        }
    }
}
