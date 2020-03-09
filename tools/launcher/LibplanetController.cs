using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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
        public bool GameRunning { get; set; }

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
            var updateWatcher = new UpdateWatcher(Storage, settings.DeployBranch, default);
            updateWatcher.VersionUpdated += (sender, e) => { Console.WriteLine(e.UpdatedVersion.Version); };
            await updateWatcher.StartAsync(TimeSpan.FromSeconds(3), cancellationToken);
        }

        private async Task SyncTask(LauncherSettings settings, CancellationToken cancellationToken)
        {
            PrivateKey privateKey = null;
            if (!string.IsNullOrEmpty(settings.KeyStorePath) && !string.IsNullOrEmpty(settings.Passphrase))
            {
                // TODO: get passphrase from UI, not setting file.
                var protectedPrivateKey = ProtectedPrivateKey.FromJson(File.ReadAllText(settings.KeyStorePath));
                privateKey = protectedPrivateKey.Unprotect(settings.Passphrase);
                Log.Debug($"Address derived from key store, is {privateKey.PublicKey.ToAddress()}");
            }

            var storePath = string.IsNullOrEmpty(settings.StorePath) ? DefaultStorePath : settings.StorePath;

            LibplanetNodeServiceProperties properties = new LibplanetNodeServiceProperties
            {
                AppProtocolVersion = settings.AppProtocolVersion,
                GenesisBlockPath = settings.GenesisBlockPath,
                NoMiner = settings.NoMiner,
                PrivateKey = privateKey ?? new PrivateKey(),
                IceServers = new[] {settings.IceServer}.Select(LoadIceServer),
                Peers = new[] {settings.Seed}.Select(LoadPeer),
                // FIXME: how can we validate it to use right store type?
                StorePath = storePath,
                StoreType = settings.StoreType,
            };

            var service = new NineChroniclesNodeService(properties);
            try
            {
                await service.Run(true, rpcListenHost: RpcListenHost, rpcListenPort: RpcListenPort, cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Log.Warning(e, "Background sync task was cancelled.");
            }
        }

        // TODO: download new client if there is.
        private async Task DownloadGameBinaryAsync(string gameBinaryPath, string deployBranch)
        {
            if (!Directory.Exists(gameBinaryPath))
            {
                var tempFilePath = Path.GetTempFileName();
                using var webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(Storage.GameBinaryDownloadUri(deployBranch), tempFilePath);

                // Extract binary.
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
        
            return new BoundPeer(pubKey, new DnsEndPoint(host, port), 0);
        }

        public async Task RunGame()
        {
            var setting = LoadSettings();
            var gameBinaryPath = setting.GameBinaryPath;
            if (string.IsNullOrEmpty(gameBinaryPath))
            {
                gameBinaryPath = DefaultGameBinaryPath;
            }

            await DownloadGameBinaryAsync(gameBinaryPath, setting.DeployBranch);

            RunGameProcess(gameBinaryPath);
        }

        public void RunGameProcess(string gameBinaryPath)
        {
            string commandArguments =
                $"--rpc-client --rpc-server-host {RpcServerHost} --rpc-server-port {RpcServerPort}";
            Process process = Process.Start(CurrentPlatform.ExecutableGameBinaryPath(gameBinaryPath), commandArguments);

            GameRunning = true;
            this.ActivateProperty(ctrl => ctrl.GameRunning);

            process.Exited += (sender, args) => {
                GameRunning = false;
                this.ActivateProperty(ctrl => ctrl.GameRunning);
            };
            process.EnableRaisingEvents = true;
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

        private const string SettingFileName = "launcher.json";

        private readonly string RpcServerHost = IPAddress.Loopback.ToString();

        private const int RpcServerPort = 30000;

        private const string RpcListenHost = "0.0.0.0";

        private const int RpcListenPort = RpcServerPort;
    }
}
