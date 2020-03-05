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
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using Libplanet.Standalone.Hosting;
using LiteDB;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Qml.Net;
using Serilog;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Launcher
{
    // FIXME: Memory leak.
    public class LibplanetController
    {
        private CancellationTokenSource _cancellationTokenSource;

        private LibplanetNodeService<NineChroniclesActionType> _nodeService;

        // It used in qml/Main.qml to hide and turn on some menus.
        [NotifySignal]
        public bool GameRunning { get; set; }

        public void StartSync()
        {
            if (GameRunning)
            {
                Log.Warning("Game is running. The background sync task should be exclusive with game.");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Run(async () => await SyncTask(cancellationToken), cancellationToken);
        }

        // It assumes StopSync() will be called when the background sync task is working well.
        public void StopSync()
        {
            // If it already executing, stop run and restart.
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _nodeService?.Dispose();

            _cancellationTokenSource = null;
            _nodeService = null;
        }

        public async Task SyncTask(CancellationToken cancellationToken)
        {
            var setting = LoadSetting();

            PrivateKey privateKey = null;
            if (!string.IsNullOrEmpty(setting.KeyStorePath) && !string.IsNullOrEmpty(setting.Passphrase))
            {
                // TODO: get passphrase from UI, not setting file.
                var protectedPrivateKey = ProtectedPrivateKey.FromJson(File.ReadAllText(setting.KeyStorePath));
                privateKey = protectedPrivateKey.Unprotect(setting.Passphrase);
                Log.Debug($"Address derived from key store, is {privateKey.PublicKey.ToAddress()}");
            }

            var storePath = string.IsNullOrEmpty(setting.StorePath) ? DefaultStorePath : setting.StorePath;

            LibplanetNodeServiceProperties properties = new LibplanetNodeServiceProperties
            {
                AppProtocolVersion = setting.AppProtocolVersion,
                GenesisBlockPath = setting.GenesisBlockPath,
                NoMiner = setting.NoMiner,
                PrivateKey = privateKey ?? new PrivateKey(),
                IceServers = new[] {setting.IceServer}.Select(LoadIceServer),
                Peers = new[] {setting.Seed}.Select(LoadPeer),
                StorePath = storePath,
                StoreType = setting.StoreType,
            };

            // BlockPolicy shared through Lib9c.
            IBlockPolicy<PolymorphicAction<ActionBase>> blockPolicy = BlockPolicy.GetPolicy();

            // FIXME: Is it needed to mine in background mode?
            Func<BlockChain<NineChroniclesActionType>, Swarm<NineChroniclesActionType>, PrivateKey, CancellationToken,
                Task> minerLoopAction =
                async (chain, swarm, privateKey, cancellationToken) =>
                {
                    var miner = new Miner(chain, swarm, privateKey);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Log.Debug("Miner called.");
                        try
                        {
                            await miner.MineBlockAsync(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Exception occurred while mining.");
                        }
                    }
                };

            _nodeService =
                new LibplanetNodeService<NineChroniclesActionType>(properties, blockPolicy, minerLoopAction);
            try
            {
                await _nodeService.StartAsync(cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Log.Warning(e, "Background sync task was cancelled.");
            }
            finally
            {
                _nodeService.Dispose();
            }
        }

        // TODO: download new client if there is.
        private async Task DownloadGameBinaryAsync(string gameBinaryPath)
        {
            if (!Directory.Exists(gameBinaryPath))
            {
                var tempFilePath = Path.GetTempFileName();
                using var webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(GameBinaryDownloadUrl, tempFilePath);

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
            var setting = LoadSetting();
            var gameBinaryPath = setting.GameBinaryPath;
            if (string.IsNullOrEmpty(gameBinaryPath))
            {
                gameBinaryPath = DefaultGameBinaryPath;
            }

            await DownloadGameBinaryAsync(gameBinaryPath);

            StopSync();

            RunGameProcess(gameBinaryPath);
        }

        public void RunGameProcess(string gameBinaryPath)
        {
            Process process = Process.Start(CurrentPlatform.ExecutableGameBinaryPath(gameBinaryPath));

            GameRunning = true;
            this.ActivateProperty(ctrl => ctrl.GameRunning);

            process.Exited += (sender, args) => {
                GameRunning = false;
                this.ActivateProperty(ctrl => ctrl.GameRunning);

                // Restart the background sync task.
                StartSync();
            };
            process.EnableRaisingEvents = true;
        }

        // NOTE: called by *settings* menu
        public void OpenSettingFile()
        {
            InitializeSettingFile();
            Process.Start(CurrentPlatform.OpenCommand, SettingFilePath);
        }

        public LauncherSetting LoadSetting()
        {
            InitializeSettingFile();
            return JsonSerializer.Deserialize<LauncherSetting>(
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
        
        private static IRuntimePlatform CurrentPlatform =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? new OSXPlatform() as IRuntimePlatform
                : RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? new WindowsPlatform() as IRuntimePlatform
                    : throw new PlatformNotSupportedException();

        private static string DefaultStorePath => Path.Combine(PlanetariumApplicationPath, "9c");

        private static string DefaultGameBinaryPath => Path.Combine(PlanetariumApplicationPath, "game");

        private static string PlanetariumApplicationPath => Path.Combine(LocalApplicationDataPath, "planetarium");

        private static string LocalApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        private static string SettingFilePath => Path.Combine(PlanetariumApplicationPath, SettingFileName);

        private const string SettingFileName = "launcher.json";

        private const string S3Host = "https://9c-test.s3.ap-northeast-2.amazonaws.com";
        
        private string GameBinaryDownloadUrl =>
            $"{S3Host}/{CurrentPlatform.GameBinaryDownloadFilename}";
    }
}
