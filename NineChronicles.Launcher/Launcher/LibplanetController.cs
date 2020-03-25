using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Common;
using Launcher.Common.Storage;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using NineChronicles.Standalone;
using Qml.Net;
using Serilog;
using static Launcher.Common.RuntimePlatform.RuntimePlatform;
using static Launcher.Common.Configuration;

namespace Launcher
{
    // FIXME: Memory leak.
    public class LibplanetController
    {
        private CancellationTokenSource _cancellationTokenSource;

        private S3Storage Storage { get; }

        // It used in qml/Main.qml to hide and turn on some menus.
        [NotifySignal]
        public bool GameRunning => GameProcess?.HasExited ?? false;

        [NotifySignal]
        public bool Updating { get; private set; }

        [NotifySignal]
        // FIXME: which name better for a flag which notices that
        //        bootstrapping and preloading ended up?
        public bool Preprocessing { get; private set; }

        private Process GameProcess { get; set; }

        [NotifySignal]
        public PrivateKey PrivateKey { get; set; }

        private string PrivateKeyHex => ByteUtil.Hex(PrivateKey.ByteArray);

        public KeyStore KeyStore => new KeyStore(LoadKeyStorePath(LoadSettings()));

        public LibplanetController()
        {
            Storage = new S3Storage();
        }

        public void StartSync()
        {
            if (GameRunning)
            {
                Log.Warning("Game is running. The background sync task should be exclusive with game.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var settings = LoadSettings();
                        await Task.WhenAll(
                            UpdateCheckTask(settings, cancellationToken),
                            SyncTask(settings, cancellationToken));
                    }
                    catch (TimeoutException e)
                    {
                        Log.Error(e, "timeout occurred.");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Unexpected exception occurred.");
                        throw;
                    }
                }
            }, cancellationToken);
        }

        // It assumes StopSync() will be called when the background sync task is working well.
        public void StopSync()
        {
            // If it already executing, stop run and restart.
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = null;
        }

        public bool Login(string addressHex, string passphrase)
        {
            var address = new Address(addressHex);
            var protectedPrivateKey = KeyStore.ProtectedPrivateKeys[address];
            try
            {
                PrivateKey = protectedPrivateKey.Unprotect(passphrase);
                this.ActivateProperty(ctrl => ctrl.PrivateKey);
                return true;
            }
            catch (Exception e) when (e is IncorrectPassphraseException ||
                                      e is MismatchedAddressException)
            {
                return false;
            }
        }

        private async Task UpdateCheckTask(LauncherSettings settings, CancellationToken cancellationToken)
        {
            // TODO: save current version in local file and load, and use it.
            var updateWatcher = new UpdateWatcher(Storage, settings.DeployBranch, LocalCurrentVersion ?? default);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            updateWatcher.VersionUpdated += async (sender, e) =>
            {
                Restart();
            };
            await updateWatcher.StartAsync(TimeSpan.FromSeconds(3), cancellationToken);
        }

        private async Task SyncTask(LauncherSettings settings, CancellationToken cancellationToken)
        {
            Preprocessing = true;
            this.ActivateProperty(ctrl => ctrl.Preprocessing);

            var storePath = string.IsNullOrEmpty(settings.StorePath) ? DefaultStorePath : settings.StorePath;
            var appProtocolVersion = AppProtocolVersion.FromToken(settings.AppProtocolVersionToken);
            var trustedAppProtocolVersionSigners = settings.TrustedAppProtocolVersionSigners
                .Select(hex => new PublicKey(ByteUtil.ParseHex(hex)))
                .ToImmutableHashSet();

            LibplanetNodeServiceProperties properties = new LibplanetNodeServiceProperties
            {
                AppProtocolVersion = appProtocolVersion,
                GenesisBlockPath = settings.GenesisBlockPath,
                NoMiner = settings.NoMiner,
                PrivateKey = PrivateKey ?? new PrivateKey(),
                IceServers = new[] {settings.IceServer}.Select(LoadIceServer),
                Peers = new[] {settings.Seed}.Select(LoadPeer),
                // FIXME: how can we validate it to use right store type?
                StorePath = storePath,
                StoreType = settings.StoreType,
                MinimumDifficulty = settings.MinimumDifficulty,
                TrustedAppProtocolVersionSigners = trustedAppProtocolVersionSigners,
            };

            var rpcProperties = new RpcNodeServiceProperties
            {
                RpcServer = true,
                RpcListenHost = RpcListenHost,
                RpcListenPort = RpcListenPort,
            };

            var service = new NineChroniclesNodeService(properties, rpcProperties);
            try
            {
                await Task.WhenAll(
                    service.Run(cancellationToken),
                    Task.Run(async () =>
                    {
                        await service.BootstrapEnded.WaitAsync(cancellationToken);
                        await service.PreloadEnded.WaitAsync(cancellationToken);

                        Preprocessing = false;
                        this.ActivateProperty(ctrl => ctrl.Preprocessing);
                    }));
            }
            catch (OperationCanceledException e)
            {
                Log.Warning(e, "Background sync task was cancelled.");
            }
            catch (DifferentAppProtocolVersionException)
            {
                // FIXME: It should notice game will be shut down!
                // It assumes another like updater, will run this, Launcher.
                // FIXME: determine updater path.
                Restart();
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception occurred: {errorMessage}", e.Message);
            }
        }

        public void RunGameProcess()
        {
            string commandArguments =
                $"--rpc-client --rpc-server-host {RpcServerHost} --rpc-server-port {RpcServerPort} --private-key {PrivateKeyHex}";
            try
            {
                GameProcess =
                    Process.Start(CurrentPlatform.ExecutableGameBinaryPath, commandArguments);
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception: {msg}", e.Message);
            }
            GameProcess.OutputDataReceived += (sender, args) => { Console.WriteLine(args.Data); };

            this.ActivateProperty(ctrl => ctrl.GameRunning);

            GameProcess.Exited += (sender, args) => {
                this.ActivateProperty(ctrl => ctrl.GameRunning);
            };
            GameProcess.EnableRaisingEvents = true;
        }

        private void StopGameProcess(CancellationToken cancellationToken)
        {
            GameProcess?.Kill(true);
        }

        // NOTE: called by *settings* menu
        public void OpenSettingFile()
        {
            InitializeSettingFile();
            Process.Start(CurrentPlatform.OpenCommand, SettingFilePath);
        }

        private void Restart()
        {
            // TODO: It should notice it will be shut down because of updates.
            const string updaterFilename = "Launcher.Updater";
            string updaterPath =
                Path.Combine(CurrentPlatform.CurrentWorkingDirectory, updaterFilename);
            GameProcess?.Kill();
            Process.Start(updaterPath);
            Environment.Exit(0);
        }

        private readonly string RpcServerHost = IPAddress.Loopback.ToString();

        private const int RpcServerPort = 30000;

        private const string RpcListenHost = "0.0.0.0";

        private const int RpcListenPort = RpcServerPort;
    }
}
