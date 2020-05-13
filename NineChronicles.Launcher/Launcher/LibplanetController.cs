using System;
using System.Collections.Generic;
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
using static Launcher.Common.Utils;
using Nekoyume;
using TextCopy;

namespace Launcher
{
    // FIXME: Memory leak.
    [Signal("quit")]
    [Signal("fatalError", NetVariantType.String, NetVariantType.Bool)]
    public class LibplanetController
    {
        private CancellationTokenSource _cancellationTokenSource;

        // Copied from UI_LOGIN_CONTENT in nekoyume/Assets/Resources/Localizations/common.csv
        [NotifySignal]
        public string WelcomeMessage =>
            @"This is a community-powered
fantasy world that fully runs on blockchain.

It is a fantasy world on the blockchain.
To start the game, you need to create your account.";

        // It used in qml/Main.qml to hide and turn on some menus.
        [NotifySignal]
        public bool GameRunning => !(GameProcess?.HasExited ?? true);

        [NotifySignal]
        public bool KeyStoreEmpty => !KeyStore.ListIds().Any();

        [NotifySignal]
        public List<string> KeyStoreOptions =>
            KeyStore.List().Select(pair => pair.Item2.Address.ToHex()).ToList();

        [NotifySignal]
        public bool Updating { get; private set; }

        [NotifySignal]
        // FIXME: which name better for a flag which notices that
        //        bootstrapping and preloading ended up?
        public bool Preprocessing { get; private set; }

        [NotifySignal]
        public bool DownloadingBlockchainSnapshot { get; private set; }

        [NotifySignal]
        public double BlockchainSnapshotDownloadProgress { get; private set; }

        private Process GameProcess { get; set; }

        [NotifySignal]
        public PrivateKey PrivateKey { get; set; }

        [NotifySignal]
        public PrivateKey PreparedPrivateKey { get; } = new PrivateKey();

        [NotifySignal]
        public Peer CurrentNode { get; set; }

        [NotifySignal]
        public string CurrentNodeAddress => CurrentNode is BoundPeer p && p.EndPoint is DnsEndPoint e
            ? $"{ByteUtil.Hex(p.PublicKey.Format(true))},{e.Host},{e.Port}"
            : null;

        [NotifySignal]
        public string PreloadStatus { get; private set; }

        [NotifySignal]
        public string PreparedPrivateKeyAddressHex => PreparedPrivateKey.ToAddress().ToHex();

        private string PrivateKeyHex => ByteUtil.Hex(PrivateKey.ByteArray);


        public IKeyStore KeyStore
        {
            get
            {
                LauncherSettings settings = LoadSettings();
                return string.IsNullOrEmpty(settings.KeyStorePath)
                    ? Web3KeyStore.DefaultKeyStore
                    : new Web3KeyStore(settings.KeyStorePath);
            }
        }

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
                try
                {
                    var settings = LoadSettings();
                    await SyncTask(settings, cancellationToken);
                }
                catch (InvalidGenesisBlockException e)
                {
                    FatalError(e, "The network to connect and this game app do not have the same genesis block.", false);
                }
                catch (TimeoutException e)
                {
                    FatalError(e, "Timed out to connect to the network.", true);
                }
                catch (Exception e)
                {
                    FatalError(e, "Unexpected exception occurred during trying to connect to the network.", true);
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
            (_, ProtectedPrivateKey protectedPrivateKey) = FindKey(addressHex);
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

        public void RevokeKey(string addressHex)
        {
            (Guid keyId, _) = FindKey(addressHex);
            KeyStore.Remove(keyId);
            this.ActivateProperty(ctrl => ctrl.KeyStoreOptions);
            this.ActivateProperty(ctrl => ctrl.KeyStoreEmpty);
        }

        private (Guid, ProtectedPrivateKey) FindKey(string addressHex)
        {
            var address = new Address(addressHex);
            foreach (Tuple<Guid, ProtectedPrivateKey> pair in KeyStore.List())
            {
                if (pair.Item2.Address.Equals(address))
                {
                    return pair.ToValueTuple();
                }
            }

            throw new KeyNotFoundException("No key of such address: " + addressHex);
        }

        private bool NewAppProtocolVersionEncountered(
            Peer peer,
            AppProtocolVersion peerVersion,
            AppProtocolVersion localVersion)
        {
            if (localVersion.Version >= peerVersion.Version)
            {
                // 상대 버전이 같거나 더 낮으면 그냥 무시.
                // TODO: 게임 쪽 코드의 Agent.DifferentAppProtocolVersionEncountered() 메서드와 기본적인
                // 로직은 같지만 구체적으로 취해야 할 액션이 크게 달라서 코드 공유를 하지 못하고 있음. 판단 로직과 판단에 따른
                // 행동 로직을 분리해서 판단 부분은 코드를 공유할 필요가 있음.
                return localVersion.Version > peerVersion.Version;
            }

            // FIXME: It should notice game will be shut down!
            // It assumes another like updater, will run this, Launcher.
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

            var rng = new Random();
            var peers = settings.Peers
                .OfType<string>()
                .OrderBy(_ => rng.Next())
                .Select(LoadPeer)
                .ToList();
            var iceServers = settings.IceServers
                .OfType<string>()
                .OrderBy(_ => rng.Next())
                .Select(LoadIceServer)
                .ToList();
            
            IImmutableSet<Address> trustedStateValidators;
            if (settings.NoTrustedStateValidators)
            {
                trustedStateValidators = ImmutableHashSet<Address>.Empty;
            }
            else
            {
                trustedStateValidators = peers.Select(p => p.Address).ToImmutableHashSet();
            }

            var properties = new LibplanetNodeServiceProperties
            {
                AppProtocolVersion = appProtocolVersion,
                GenesisBlockPath = settings.GenesisBlockPath,
                NoMiner = settings.NoMiner,
                PrivateKey = PrivateKey ?? new PrivateKey(),
                IceServers = iceServers,
                Peers = peers,
                TrustedStateValidators = trustedStateValidators,
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

            TelemetryClient.Context.User.AuthenticatedUserId = service.Swarm.Address.ToHex();

            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        await Task.Delay(100, cancellationToken);
                        if (File.Exists(CurrentPlatform.RunCommandFilePath))
                        {
                            if (!GameRunning && !Preprocessing)
                            {
                                RunGameProcess();
                            }
                            File.Delete(CurrentPlatform.RunCommandFilePath);
                        }
                    }
                },
                cancellationToken);

            Task.Run(
                async () =>
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
                        await Task.Delay(1000, cancellationToken);
                        currentNode = service.Swarm.AsPeer;
                    }
                    while (!(currentNode is BoundPeer));

                    CurrentNode = currentNode;
                    Log.Information("Current node address: {0}", CurrentNodeAddress);
                    this.ActivateProperty(ctrl => ctrl.CurrentNode);
                    this.ActivateProperty(ctrl => ctrl.CurrentNodeAddress);
                },
                cancellationToken);

            await service.Run(cancellationToken);
        }

        private Address ToAddress(string hexstring)
        {
            if (hexstring.StartsWith("0x"))
            {
                hexstring = hexstring.Substring(2);
            }

            return new Address(ByteUtil.ParseHex(hexstring));
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

        public void CopyClipboard(string value)
        {
            Clipboard.SetText(value);
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
            Process.Start(CurrentPlatform.OpenCommand, EscapeShellArgument(SettingFilePath));
        }

        // Advanced → Clear cache 메뉴가 호출
        public void ClearStore()
        {
            LauncherSettings settings = LoadSettings();
            string storePath = string.IsNullOrEmpty(settings?.StorePath)
                ? DefaultStorePath
                : settings.StorePath;

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

            ActivateQuitSignal();
        }

        public void DownloadBlockchainSnapshot()
        {
            DownloadingBlockchainSnapshot = true;
            this.ActivateProperty(ctrl => ctrl.DownloadingBlockchainSnapshot);
            Task.Run(async () =>
            {
                await Downloader.DownloadBlockchainSnapshot(new BlockchainSnapshotDownloadProgressBar(this));
                ActivateQuitSignal();
            });
        }

        private class BlockchainSnapshotDownloadProgressBar : IProgress<(long Downloaded, long Total)>
        {
            private LibplanetController _ctrl;

            public BlockchainSnapshotDownloadProgressBar(LibplanetController ctrl)
            {
                _ctrl = ctrl;
            }

            public void Report((long Downloaded, long Total) value)
            {
                _ctrl.BlockchainSnapshotDownloadProgress =
                    (double) value.Downloaded / (double) value.Total;
                _ctrl.ActivateProperty(ctrl => ctrl.BlockchainSnapshotDownloadProgress);
            }
        }

        public void CreatePrivateKey(string passphrase)
        {
            PrivateKey = PreparedPrivateKey;
            ProtectedPrivateKey ppk = ProtectedPrivateKey.Protect(PrivateKey, passphrase);
            KeyStore.Add(ppk);
            this.ActivateProperty(ctrl => ctrl.PrivateKey);
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

        private void FatalError(Exception exception, string message, bool retryable)
        {
            ActivateFatalErrorSignal(message, retryable);
            Log.Error(exception, message);
        }

        private void RestartToUpdate(Nekoyume.AppProtocolVersionExtra extra)
        {
            // TODO: It should notice it will be shut down because of updates.
            string binaryUrl = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? extra.MacOSBinaryUrl
                : extra.WindowsBinaryUrl;
            var procInfo = new ProcessStartInfo(CurrentPlatform.ExecutableUpdaterBinaryPath)
            {
                Arguments = binaryUrl,
                UseShellExecute = true,
            };

            Process.Start(procInfo);
            // NOTE: Environment.Exit(int)에 Qt Thread가 반응하지 않아 Qt 쪽에서 프로세스 종료를 처리하게 합니다.
            ActivateQuitSignal();
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

        private void ActivateQuitSignal()
        {
            this.ActivateSignal("quit");
        }

        private void ActivateFatalErrorSignal(string message, bool retryable)
        {
            this.ActivateSignal("fatalError", message, retryable);
        }

        private readonly string RpcServerHost = IPAddress.Loopback.ToString();

        private const string RpcListenHost = "0.0.0.0";

        private int RpcServerPort;
    }
}
