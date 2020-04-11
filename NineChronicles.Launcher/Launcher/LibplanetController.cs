using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Launcher.Common;
using Libplanet;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using NineChronicles.Standalone;
using Qml.Net;
using Serilog;
using static Launcher.Common.RuntimePlatform.RuntimePlatform;
using static Launcher.Common.Configuration;
using Nekoyume;

namespace Launcher
{
    // FIXME: Memory leak.
    [Signal("quit")]
    [Signal("fatalError", NetVariantType.String)]
    public class LibplanetController
    {
        private CancellationTokenSource _cancellationTokenSource;

        // It used in qml/Main.qml to hide and turn on some menus.
        [NotifySignal]
        public bool GameRunning => !(GameProcess?.HasExited ?? true);

        [NotifySignal]
        public bool Updating { get; private set; }

        [NotifySignal]
        // FIXME: which name better for a flag which notices that
        //        bootstrapping and preloading ended up?
        public bool Preprocessing { get; private set; }

        private Process GameProcess { get; set; }

        [NotifySignal]
        public PrivateKey PrivateKey { get; set; }

        [NotifySignal]
        public Peer CurrentNode { get; set; }

        [NotifySignal]
        public string CurrentNodeAddress => CurrentNode is BoundPeer p && p.EndPoint is DnsEndPoint e
            ? $"{ByteUtil.Hex(p.PublicKey.Format(true))},{e.Host},{e.Port}"
            : null;

        [NotifySignal]
        public string PreloadStatus { get; private set; }

        private string PrivateKeyHex => ByteUtil.Hex(PrivateKey.ByteArray);

        public KeyStore KeyStore => new KeyStore(LoadKeyStorePath(LoadSettings()));

        public LibplanetController()
        {
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
                        await SyncTask(settings, cancellationToken);
                    }
                    catch (InvalidGenesisBlockException e)
                    {
                        FatalError(e, "The network to connect and this game app do not have the same genesis block.");
                        return;
                    }
                    catch (TimeoutException e)
                    {
                        FatalError(e, "Timed out to connect to the network.");
                        return;
                    }
                    catch (Exception e)
                    {
                        FatalError(e, "Unexpected exception occurred during trying to connect to the network.");
                        return;
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

        private bool NewAppProtocolVersionEncountered(
            Peer peer,
            AppProtocolVersion peerVersion,
            AppProtocolVersion localVersion)
        {
            // FIXME: It should notice game will be shut down!
            // It assumes another like updater, will run this, Launcher.
            // FIXME: determine updater path.
            Log.Information("A new version is available: {Version}", peerVersion);
            var extra = new Nekoyume.AppProtocolVersionExtra((Bencodex.Types.Dictionary) peerVersion.Extra);
            RestartToUpdate(extra);
            return false;
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
                Peers = new[] {settings.Seed}.Where(a => a is string).Select(LoadPeer),
                // FIXME: how can we validate it to use right store type?
                StorePath = storePath,
                StoreType = settings.StoreType,
                MinimumDifficulty = settings.MinimumDifficulty,
                TrustedAppProtocolVersionSigners = trustedAppProtocolVersionSigners,
                DifferentAppProtocolVersionEncountered = NewAppProtocolVersionEncountered,
            };

            RpcServerPort = GetFreeTcpPort();
            var rpcProperties = new RpcNodeServiceProperties
            {
                RpcServer = true,
                RpcListenHost = RpcListenHost,
                RpcListenPort = RpcServerPort
            };

            var service = new NineChroniclesNodeService(
                properties,
                rpcProperties,
                new Progress<PreloadState>(preloadState =>
                {
                    PreloadStatus = CreatePreloadStateDescription(preloadState);
                    this.ActivateProperty(ctrl => ctrl.PreloadStatus);
                })
            );
            try
            {
                await Task.WhenAll(
                    service.Run(cancellationToken),
                    Task.Run(async () =>
                    {
                        PreloadStatus = "Connecting to the network...";
                        this.ActivateProperty(ctrl => ctrl.PreloadStatus);

                        if (properties.Peers.Any())
                        {
                            await service.BootstrapEnded.WaitAsync(cancellationToken);
                            Log.Information("Bootstrap Ended");

                            await service.PreloadEnded.WaitAsync(cancellationToken);
                            Log.Information("Preload Ended");
                        }

                        Preprocessing = false;
                        this.ActivateProperty(ctrl => ctrl.Preprocessing);

                        Peer currentNode = null;
                        do
                        {
                            await Task.Delay(1000);
                            currentNode = service.Swarm.AsPeer;
                        }
                        while (currentNode is null);

                        CurrentNode = currentNode;
                        Log.Information("Current node address: {0}", CurrentNodeAddress);
                        this.ActivateProperty(ctrl => ctrl.CurrentNode);
                        this.ActivateProperty(ctrl => ctrl.CurrentNodeAddress);
                    }));
            }
            catch (OperationCanceledException e)
            {
                Log.Warning(e, "Background sync task was cancelled.");
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception occurred: {errorMessage}", e.Message);
            }
        }

        private int GetFreeTcpPort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                l.Start();
                return ((IPEndPoint) l.LocalEndpoint).Port;
            }
            finally
            {
                l.Stop();
            }
        }

        public bool RunGameProcess()
        {
            string commandArguments =
                $"--rpc-client --rpc-server-host {RpcServerHost} --rpc-server-port {RpcServerPort} --private-key {PrivateKeyHex}";

            // QML에서 호출되는 함수이므로 예외처리를 안에서 합니다.
            try
            {
                GameProcess =
                    Process.Start(CurrentPlatform.ExecutableGameBinaryPath, commandArguments);
                GameProcess.OutputDataReceived += (sender, args) => { Console.WriteLine(args.Data); };

                this.ActivateProperty(ctrl => ctrl.GameRunning);

                GameProcess.Exited += (sender, args) => {
                    this.ActivateProperty(ctrl => ctrl.GameRunning);
                };
                GameProcess.EnableRaisingEvents = true;

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception: {msg}", e.Message);

                return false;
            }
        }

        public void StopGameProcess()
        {
            GameProcess?.Kill(true);
        }

        // Advanced → Settings 메뉴가 호출
        public void OpenSettingFile()
        {
            InitializeSettingFile();
            Process.Start(CurrentPlatform.OpenCommand, SettingFilePath);
        }

        // Advanced → Clear cache 메뉴가 호출
        public void ClearStore()
        {
            LauncherSettings settings = LoadSettings();
            string storePath = string.IsNullOrEmpty(settings?.StorePath) ? DefaultStorePath : settings.StorePath;

            StopGameProcess();
            StopSync();
            Log.Information("Try to clear store: {0}", storePath);

            try
            {
                StoreUtils.ResetStore(storePath);
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected exception happened during clearing store.");
            }

            this.ActivateSignal("quit");
        }

        private static IceServer LoadIceServer(string iceServerInfo)
        {
            var uri = new Uri(iceServerInfo);
            string[] userInfo = uri.UserInfo.Split(':');

            return new IceServer(new[] { uri }, userInfo[0], userInfo[1]);
        }

        private static BoundPeer LoadPeer(string peerInfo)
        {
            var tokens = peerInfo.Split(',');
            var pubKey = new PublicKey(ByteUtil.ParseHex(tokens[0]));
            var host = tokens[1];
            var port = int.Parse(tokens[2]);

            return new BoundPeer(pubKey, new DnsEndPoint(host, port), default(AppProtocolVersion));
        }

        private void FatalError(Exception exception, string message)
        {
            this.ActivateSignal("fatalError", message);
            Log.Error(exception, message);
        }

        private void RestartToUpdate(Nekoyume.AppProtocolVersionExtra extra)
        {
            // TODO: It should notice it will be shut down because of updates.
            string binaryUrl = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? extra.MacOSBinaryUrl
                : extra.WindowsBinaryUrl;
            const string updaterFilename = "Launcher.Updater";
            string updaterPath =
                Path.Combine(CurrentPlatform.CurrentWorkingDirectory, updaterFilename);
            Process.Start(updaterPath, binaryUrl);
            // NOTE: Environment.Exit(int)에 Qt Thread가 반응하지 않아 Qt 쪽에서 프로세스 종료를 처리하게 합니다.
            this.ActivateSignal("quit");
        }

        private string CreatePreloadStateDescription(PreloadState state)
        {
            // FIXME 메시지 국제화 해야합니다.
            string descripiton;
            long count;
            long totalCount;

            switch (state)
            {
                case BlockHashDownloadState blockHashDownloadState:
                    descripiton = "Downloading block hashes...";
                    count = blockHashDownloadState.ReceivedBlockHashCount;
                    totalCount = blockHashDownloadState.EstimatedTotalBlockHashCount;
                    break;

                case BlockDownloadState blockDownloadState:
                    descripiton = "Downloading blocks...";
                    count = blockDownloadState.ReceivedBlockCount;
                    totalCount = blockDownloadState.TotalBlockCount;
                    break;

                case BlockVerificationState blockVerificationState:
                    descripiton = "Verifying blocks...";
                    count = blockVerificationState.VerifiedBlockCount;
                    totalCount = blockVerificationState.TotalBlockCount;
                    break;

                case StateDownloadState stateReferenceDownloadState:
                    descripiton = "Downloading states...";
                    count = stateReferenceDownloadState.ReceivedIterationCount;
                    totalCount = stateReferenceDownloadState.TotalIterationCount;
                    break;

                case ActionExecutionState actionExecutionState:
                    descripiton = "Executing actions...";
                    count = actionExecutionState.ExecutedBlockCount;
                    totalCount = actionExecutionState.TotalBlockCount;
                    break;

                default:
                    throw new Exception("Unknown state was reported during preload.");
            }

            return $"{descripiton} {count} / {totalCount} ({state.CurrentPhase} / {PreloadState.TotalPhase})";
        }

        private readonly string RpcServerHost = IPAddress.Loopback.ToString();

        private const string RpcListenHost = "0.0.0.0";

        private int RpcServerPort;
    }
}
