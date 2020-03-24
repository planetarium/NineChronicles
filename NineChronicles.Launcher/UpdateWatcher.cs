using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Storage;
using Serilog;

namespace Launcher
{
    public class UpdateWatcher
    {
        private S3Storage Storage { get; }

        public VersionDescriptor LatestVersion { get; private set; }

        public string DeployBranch { get; }

        public event EventHandler<VersionUpdatedEventArgs> VersionUpdated;

        public UpdateWatcher(S3Storage storage, string deployBranch, VersionDescriptor latestVersion)
        {
            Storage = storage;
            DeployBranch = deployBranch;
            LatestVersion = latestVersion;
        }

        public async Task StartAsync(
            TimeSpan checkInterval,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentVersion = await CurrentVersionAsync(cancellationToken);
                    if (!LatestVersion.Equals(currentVersion))
                    {
                        VersionUpdated?.Invoke(this, new VersionUpdatedEventArgs(currentVersion));
                        LatestVersion = currentVersion;
                    }

                    Log.Debug($"{nameof(UpdateWatcher)} checked.");

                    await Task.Delay(checkInterval, cancellationToken);
                }
                catch (Exception e)
                {
                    Log.Error(e, e.Message);
                }
            }
        }

        private async Task<VersionDescriptor> CurrentVersionAsync(CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            var responseMessage = await httpClient.GetAsync(Storage.VersionHistoryUri(DeployBranch), cancellationToken);
            var rawVersionHistory = await responseMessage.Content.ReadAsStringAsync();
            var versionHistory = JsonSerializer.Deserialize<VersionHistory>(
                rawVersionHistory,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });
            return versionHistory.Versions.First(descriptor => descriptor.Version == versionHistory.CurrentVersion);
        }

        public class VersionUpdatedEventArgs : EventArgs
        {
            public VersionUpdatedEventArgs(VersionDescriptor updatedVersion)
            {
                UpdatedVersion = updatedVersion;
            }

            /// <summary>
            /// The current version on version history in online storage.
            /// </summary>
            public VersionDescriptor UpdatedVersion { get; }
        }
    }
}
