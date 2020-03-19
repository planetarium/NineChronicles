using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Launcher.Storage;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using NineChronicles.Standalone;
using Qml.Net;
using Serilog;
using JsonSerializer = System.Text.Json.JsonSerializer;
using static Launcher.RuntimePlatform.RuntimePlatform;

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
                            SyncTask(settings, cancellationToken)
                        );
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

        private async Task UpdateCheckTask(LauncherSettings settings, CancellationToken cancellationToken)
        {
            // TODO: save current version in local file and load, and use it.
            var updateWatcher = new UpdateWatcher(Storage, settings.DeployBranch, LocalCurrentVersion ?? default);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            updateWatcher.VersionUpdated += async (sender, e) =>
            {
                Updating = true;
                this.ActivateProperty(ctrl => ctrl.Updating);

                var version = e.UpdatedVersion.Version;
                var tempPath = Path.Combine(Path.GetTempPath(), "temp-9c-download" + version);
                cts.Cancel();
                await DownloadGameBinaryAsync(tempPath, settings.DeployBranch, version, cts.Token);

                // FIXME: it kills game process in force, if it was running. it should be
                //        killed with some message.
                SwapGameDirectory(
                    LoadGameBinaryPath(settings),
                    Path.Combine(tempPath, "MacOS"));
                cts.Dispose();
                cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                LocalCurrentVersion = e.UpdatedVersion;

                Updating = false;
                this.ActivateProperty(ctrl => ctrl.Updating);
            };
            await updateWatcher.StartAsync(TimeSpan.FromSeconds(3), cancellationToken);
        }

        private void SwapGameDirectory(string gameBinaryPath, string newGameBinaryPath)
        {
            GameProcess?.Kill();

            if (Directory.Exists(gameBinaryPath))
            {
                Directory.Delete(gameBinaryPath, recursive: true);
            }

            Directory.Move(newGameBinaryPath, gameBinaryPath);
        }

        private async Task SyncTask(LauncherSettings settings, CancellationToken cancellationToken)
        {
            Preprocessing = true;
            this.ActivateProperty(ctrl => ctrl.Preprocessing);

            PrivateKey privateKey = null;
            if (!string.IsNullOrEmpty(settings.KeyStorePath) && !string.IsNullOrEmpty(settings.Passphrase))
            {
                // TODO: get passphrase from UI, not setting file.
                var protectedPrivateKey = ProtectedPrivateKey.FromJson(File.ReadAllText(settings.KeyStorePath));
                privateKey = protectedPrivateKey.Unprotect(settings.Passphrase);
                Log.Debug($"Address derived from key store, is {privateKey.PublicKey.ToAddress()}");
            }

            var storePath = string.IsNullOrEmpty(settings.StorePath) ? DefaultStorePath : settings.StorePath;
            var appProtocolVersion = AppProtocolVersion.FromToken(settings.AppProtocolVersionToken);

            LibplanetNodeServiceProperties properties = new LibplanetNodeServiceProperties
            {
                AppProtocolVersion = appProtocolVersion,
                GenesisBlockPath = settings.GenesisBlockPath,
                NoMiner = settings.NoMiner,
                PrivateKey = privateKey ?? new PrivateKey(),
                IceServers = new[] {settings.IceServer}.Select(LoadIceServer),
                Peers = new[] {settings.Seed}.Select(LoadPeer),
                // FIXME: how can we validate it to use right store type?
                StorePath = storePath,
                StoreType = settings.StoreType,
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
        }

        private async Task DownloadGameBinaryAsync(string gameBinaryPath, string deployBranch, string version, CancellationToken cancellationToken)
        {
            var tempFilePath = Path.GetTempFileName();
            using var httpClient = new HttpClient();
            Log.Debug(Storage.GameBinaryDownloadUri(deployBranch, version).ToString());
            var responseMessage = await httpClient.GetAsync(Storage.GameBinaryDownloadUri(deployBranch, version), cancellationToken);
            using var fileStream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write);
            await responseMessage.Content.CopyToAsync(fileStream);

            // Extract binary.
            // TODO: implement a function to extract with file extension.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await using var tempFile = File.OpenRead(tempFilePath);
                using var gz = new GZipInputStream(tempFile);
                using var tar = TarArchive.CreateInputTarArchive(gz);
                tar.ExtractContents(gameBinaryPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ZipFile.ExtractToDirectory(tempFilePath, gameBinaryPath);
            }
        }

        private static string LoadGameBinaryPath(LauncherSettings settings)
        {
            if (string.IsNullOrEmpty(settings.GameBinaryPath))
            {
                return DefaultGameBinaryPath;
            }
            else
            {
                return settings.GameBinaryPath;
            }
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

        public async Task RunGame()
        {
            var setting = LoadSettings();
            var gameBinaryPath = setting.GameBinaryPath;
            if (string.IsNullOrEmpty(gameBinaryPath))
            {
                gameBinaryPath = DefaultGameBinaryPath;
            }

            RunGameProcess(gameBinaryPath);
        }

        public void RunGameProcess(string gameBinaryPath)
        {
            string commandArguments =
                $"--rpc-client --rpc-server-host {RpcServerHost} --rpc-server-port {RpcServerPort}";
            GameProcess = Process.Start(CurrentPlatform.ExecutableGameBinaryPath(gameBinaryPath), commandArguments);

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

        public LauncherSettings LoadSettings()
        {
            InitializeSettingFile();
            return JsonSerializer.Deserialize<LauncherSettings>(
                File.ReadAllText(SettingFilePath),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }

        private void InitializeSettingFile()
        {
            if (!File.Exists(SettingFilePath))
            {
                File.Copy(Path.Combine("resources", SettingFileName), SettingFilePath);
            }
        }

        private static string DefaultStorePath => Path.Combine(PlanetariumApplicationPath, "9c");

        private static string DefaultGameBinaryPath => Path.Combine(PlanetariumApplicationPath, "game");

        private static string PlanetariumApplicationPath => Path.Combine(LocalApplicationDataPath, "planetarium");

        private static string LocalApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        private static string SettingFilePath => Path.Combine(PlanetariumApplicationPath, SettingFileName);

        private VersionDescriptor? LocalCurrentVersion
        {
            get
            {
                try
                {
                    var raw = File.ReadAllText(LocalCurrentVersionPath);
                    return JsonSerializer.Deserialize<VersionDescriptor>(
                        raw,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        });
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Unexpected exception occurred: {e.Message}");
                    return null;
                }
            }
            set => File.WriteAllText(LocalCurrentVersionPath, JsonSerializer.Serialize((VersionDescriptor) value,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }));
        }

        private string LocalCurrentVersionPath => Path.Combine(PlanetariumApplicationPath, "9c-current-version.json");

        private const string SettingFileName = "launcher.json";

        private readonly string RpcServerHost = IPAddress.Loopback.ToString();

        private const int RpcServerPort = 30000;

        private const string RpcListenHost = "0.0.0.0";

        private const int RpcListenPort = RpcServerPort;
    }
}
