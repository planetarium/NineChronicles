using System;
using System.Linq;
using System.Net;
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
                    var currentVersion = await CurrentVersionAsync();
                    if (!LatestVersion.Equals(currentVersion))
                    {
                        VersionUpdated?.Invoke(this, new VersionUpdatedEventArgs(currentVersion));
                        LatestVersion = currentVersion;
                    }

                    await Task.Delay(checkInterval, cancellationToken);
                }
                catch (Exception e)
                {
                    Log.Error(e, e.Message);
                }
            }
        }

        private async Task<VersionDescriptor> CurrentVersionAsync()
        {
            using var webClient = new WebClient();
            var rawVersionHistory = await webClient.DownloadStringTaskAsync(Storage.VersionHistoryUri(DeployBranch));
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
