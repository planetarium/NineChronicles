using System;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Common;
using Launcher.Common.Storage;
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
                    var currentVersion = await VersionHelper.CurrentVersionAsync(Storage, DeployBranch, cancellationToken);
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
