using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Common.Storage;

namespace Launcher.Common
{
    public static class VersionHelper
    {
        public static async Task<VersionDescriptor> CurrentVersionAsync(
            S3Storage storage,
            string deployBranch,
            CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            var responseMessage = await httpClient.GetAsync(storage.VersionHistoryUri(deployBranch),
                cancellationToken);
            var rawVersionHistory = await responseMessage.Content.ReadAsStringAsync();
            var versionHistory = JsonSerializer.Deserialize<VersionHistory>(
                rawVersionHistory,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
            return versionHistory.Versions.First(descriptor =>
                descriptor.Version == versionHistory.CurrentVersion);
        }
    }
}
