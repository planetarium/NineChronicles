using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using Nekoyume.Multiplanetary;
using UnityEngine;

#if UNITY_EDITOR || !UNITY_ANDROID
using System.IO;
#endif

namespace Nekoyume.Helper
{
    [Serializable]
    public class CommandLineOptions
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            AllowTrailingCommas = true,
            Converters =
            {
                new StringEnumerableConverter(),
                new NullablePlanetIdJsonConverter(),
            },
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private string _planetRegistryUrl;

        private string _defaultPlanetId;

        private string _selectedPlanetId;

        private string privateKey;

        // null이면 Web3KeyStore.DefaultStore 따름
        private string keyStorePath;

        private string host;

        private int port;

        private bool noMiner;

        private string[] peers = new string[] { };

        private string[] iceServers = new string[] { };

        private string storagePath;

        private string storageType;

        private bool rpcClient;

        private string rpcServerHost;

        private string[] rpcServerHosts = { };

        private int rpcServerPort;

        private bool autoPlay;

        private bool consoleSink;

        private bool development;

        private bool maintenance;

        private bool testEnd;

        private string appProtocolVersion;

        private string[] trustedAppProtocolVersionSigners = new string[] { };

        private string language;

        private string awsSinkGuid;

        private string apiServerHost;

        private string onBoardingHost;

        private double sentrySampleRate = 0;

        private string marketServiceHost;

        private string meadPledgePortalUrl;

        private string iapServiceHost;

        private bool ingameDebugConsole;

        private string _patrolRewardServiceHost;

        private string _seasonPassServiceHost;

        private string _googleMarketUrl;

        private string _appleMarketUrl;

        private bool _requiredUpdate;

        private string _guildServiceUrl;

        private string _guildIconBucket;

        private bool _enableGuestLogin;

        public bool Empty { get; private set; } = true;

        public string genesisBlockPath;

        [Option("planet-registry-url", Required = false, HelpText = "planet registry url")]
        public string PlanetRegistryUrl
        {
            get => _planetRegistryUrl;
            set
            {
                _planetRegistryUrl = value;
                Empty = false;
            }
        }

        /// <summary>
        /// Use this if you want to set the default planet id which is used
        ///  earlier than the <see cref="PlanetSelector.DefaultPlanetId"/>.
        /// This is used if there is no selected planet id in the player prefs.
        /// PlayerPrefs key: <see cref="PlanetSelector.CachedPlanetIdStringKey"/>
        /// </summary>
        [Option("default-planet-id", Required = false, HelpText = "planet id")]
        public string DefaultPlanetId
        {
            get => _defaultPlanetId;
            set
            {
                _defaultPlanetId = value;
                Empty = false;
            }
        }

        /// <summary>
        /// Use this if you want to inject the selected planet id.
        /// It is usually used for standalone platforms with the launcher.
        /// </summary>
        [Option("selected-planet-id", Required = false, HelpText = "planet id")]
        public string SelectedPlanetId
        {
            get => _selectedPlanetId;
            set
            {
                _selectedPlanetId = value;
                Empty = false;
            }
        }

        [Option("private-key", Required = false, HelpText = "The private key to use.")]
        public string PrivateKey
        {
            get => privateKey;
            set
            {
                privateKey = value;
                Empty = false;
            }
        }

        [Option("keystore-path", Required = false, HelpText = "The keystore path.")]
        public string KeyStorePath
        {
            get => keyStorePath;
            set
            {
                keyStorePath = value;
                Empty = false;
            }
        }

        [Option("host", Required = false, HelpText = "The host name to use.")]
        public string Host
        {
            get => host;
            set
            {
                host = value;
                Empty = false;
            }
        }

        [Option("port", Required = false, HelpText = "The source port to use.")]
        public int? Port
        {
            get => port == 0 ? default(int?) : port;
            set
            {
                port = value ?? 0;
                Empty = false;
            }
        }

        [Option("no-miner", Required = false, HelpText = "Do not mine block.")]
        public bool NoMiner
        {
            get => noMiner;
            set
            {
                noMiner = value;
                Empty = false;
            }
        }

        [Option("ice-servers", Required = false, HelpText = "STUN/TURN servers to use. (Usage: --ice-servers serverA serverB ...)")]
        public IEnumerable<string> IceServers
        {
            get => iceServers;
            set
            {
                iceServers = value.ToArray();
                if (value.Any())
                {
                    Empty = false;
                }
            }
        }

        [Option("peer", Required = false, HelpText = "Peers to add. (Usage: --peer peerA peerB ...)")]
        public IEnumerable<string> Peers
        {
            get => peers;
            set
            {
                peers = value.ToArray();
                if (value.Any())
                {
                    Empty = false;
                }
            }
        }

        [Option("storage-path", Required = false, HelpText = "The path to store game data.")]
        public string StoragePath
        {
            get => storagePath;
            set
            {
                storagePath = value;
                Empty = false;
            }
        }

        [Option("storage-type", Required = false, HelpText = "The storage type to use.")]
        public string StorageType
        {
            get => storageType;
            set
            {
                storageType = value;
                Empty = false;
            }
        }

        [Option("rpc-client", Required = false, HelpText = "Play in client mode.")]
        public bool RpcClient
        {
            get => rpcClient;
            set
            {
                rpcClient = value;
                Empty = false;
            }
        }

        [Option("rpc-server-host", Required = false, HelpText = "The host name for client mode.")]
        public string RpcServerHost
        {
            get => rpcServerHost;
            set
            {
                rpcServerHost = value;
                Empty = false;
            }
        }

        [Option("rpc-server-hosts", Required = false, HelpText = "The host names for client mode.")]
        public IEnumerable<string> RpcServerHosts
        {
            get => rpcServerHosts;
            set
            {
                rpcServerHosts = value.ToArray();
                if (value.Any())
                {
                    Empty = false;
                }
            }
        }

        [Option("rpc-server-port", Required = false, HelpText = "The port number for client mode.")]
        public int RpcServerPort
        {
            get => rpcServerPort;
            set
            {
                rpcServerPort = value;
                Empty = false;
            }
        }

        [Option("auto-play", Required = false, HelpText = "Play automatically in background.")]
        public bool AutoPlay
        {
            get => autoPlay;
            set
            {
                autoPlay = value;
                Empty = false;
            }
        }

        [Option("console-sink", Required = false, HelpText = "Write logs on console.")]
        public bool ConsoleSink
        {
            get => consoleSink;
            set
            {
                consoleSink = value;
                Empty = false;
            }
        }

        [Option("development", Required = false, HelpText = "Turn on development mode.")]
        public bool Development
        {
            get => development;
            set
            {
                development = value;
                Empty = false;
            }
        }

        [Option("maintenance", Required = false, HelpText = "Turn on maintenance mode.")]
        public bool Maintenance
        {
            get => maintenance;
            set
            {
                maintenance = value;
                Empty = false;
            }
        }

        [Option("testEnd", Required = false, HelpText = "Test has ended.")]
        public bool TestEnd
        {
            get => testEnd;
            set
            {
                testEnd = value;
                Empty = false;
            }
        }

        [Option('V', "app-protocol-version",
            Required = false,
            HelpText = "App protocol version token.")]
        public string AppProtocolVersion
        {
            get => appProtocolVersion;
            set
            {
                appProtocolVersion = value;
                Empty = false;
            }
        }

        [Option('T', "trusted-app-protocol-version-signer",
            Required = false,
            HelpText = "Trustworthy signers who claim new app protocol versions")]
        public IEnumerable<string> TrustedAppProtocolVersionSigners
        {
            get => trustedAppProtocolVersionSigners;
            set
            {
                trustedAppProtocolVersionSigners = value.ToArray();
                if (trustedAppProtocolVersionSigners.Any())
                {
                    Empty = false;
                }
            }
        }

        [Option("genesis-block-path", Required = false)]
        public string GenesisBlockPath
        {
            get => genesisBlockPath;
            set
            {
                genesisBlockPath = value;
                Empty = false;
            }
        }

        [Option("language", Required = false, HelpText = "Choose language.")]
        public string Language
        {
            get => language;
            set
            {
                language = value;
                Empty = false;
            }
        }

#if UNITY_EDITOR
        [Option("aws-sink-guid", Required = false, HelpText = "Guid for aws cloudwatch logging.")]
#else
        [Option("aws-sink-guid", Required = true, HelpText = "Guid for aws cloudwatch logging.")]
#endif
        public string AwsSinkGuid
        {
            get => awsSinkGuid;
            set
            {
                awsSinkGuid = value;
                Empty = false;
            }
        }

        /// <summary>
        /// DataProvider Host.
        /// </summary>
        [Option("api-server-host", Required = false, HelpText = "Host for the internal api client.")]
        public string ApiServerHost
        {
            get => apiServerHost;
            set
            {
                apiServerHost = value;
                Empty = false;
            }
        }

        /// <summary>
        /// WorldBoss Host.
        /// </summary>
        [Option("on-boarding-host", Required = false, HelpText = "on boarding host")]
        public string OnBoardingHost
        {
            get => onBoardingHost;
            set
            {
                onBoardingHost = value;
                Empty = false;
            }
        }

        [Option("sentry-sample-rate", Required = false, HelpText = "sample rate for sentry trace")]
        public double SentrySampleRate
        {
            get => sentrySampleRate;
            set
            {
                sentrySampleRate = value;
                Empty = false;
            }
        }

        [Option("market-service-host", Required = false, HelpText = "shop service host")]
        public string MarketServiceHost
        {
            get => marketServiceHost;
            set
            {
                marketServiceHost = value;
                Empty = false;
            }
        }

        [Option("mead-pledge-portal-url", Required = false, HelpText = "mead pledge portal url")]
        public string MeadPledgePortalUrl
        {
            get => meadPledgePortalUrl;
            set
            {
                meadPledgePortalUrl = value;
                Empty = false;
            }
        }

        [Option("iap-service-host", Required = false, HelpText = "iap server host")]
        public string IAPServiceHost
        {
            get => iapServiceHost;
            set
            {
                iapServiceHost = value;
                Empty = false;
            }
        }

        [Option("ingame-debug-console", Required = false, HelpText = "Turn on ingame debug console")]
        public bool IngameDebugConsole
        {
            get => ingameDebugConsole;
            set
            {
                ingameDebugConsole = value;
                Empty = false;
            }
        }

        [Option("patrol-reward-service-host", Required = false, HelpText = "patrol reward service host")]
        public string PatrolRewardServiceHost
        {
            get => _patrolRewardServiceHost;
            set
            {
                _patrolRewardServiceHost = value;
                Empty = false;
            }
        }

        [Option("season-pass-service-host", Required = false, HelpText = "season pass service host")]
        public string SeasonPassServiceHost
        {
            get => _seasonPassServiceHost;
            set
            {
                _seasonPassServiceHost = value;
                Empty = false;
            }
        }

        [Option("google-market-url", Required = false, HelpText = "google market url")]
        public string GoogleMarketUrl
        {
            get => _googleMarketUrl;
            set
            {
                _googleMarketUrl = value;
                Empty = false;
            }
        }

        [Option("apple-market-url", Required = false, HelpText = "apple market url")]
        public string AppleMarketUrl
        {
            get => _appleMarketUrl;
            set
            {
                _appleMarketUrl = value;
                Empty = false;
            }
        }

        [Option("required-update", Required = false, HelpText = "required update")]
        public bool RequiredUpdate
        {
            get => _requiredUpdate;
            set
            {
                _requiredUpdate = value;
                Empty = false;
            }
        }

        [Option("guild-service-url", Required = false, HelpText = "guild service url")]
        public string GuildServiceUrl
        {
            get => _guildServiceUrl;
            set
            {
                _guildServiceUrl = value;
                Empty = false;
            }
        }

        [Option("guild-icon-bucket", Required = false, HelpText = "guild icon s3 bucket url")]
        public string GuildIconBucket
        {
            get => _guildIconBucket;
            set
            {
                _guildIconBucket = value;
                Empty = false;
            }
        }

        [Option("enable-guest-login", Required = false, HelpText = "enable guest login")]
        public bool EnableGuestLogin
        {
            get => _enableGuestLogin;
            set
            {
                _enableGuestLogin = value;
                Empty = false;
            }
        }

        public override string ToString()
        {
            string result = "";
            IEnumerable<PropertyInfo> properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (PropertyInfo property in properties)
            {
                OptionAttribute optAttr = Attribute.GetCustomAttribute(property, typeof(OptionAttribute)) as OptionAttribute;
                if (optAttr != null && property.GetValue(this) != null)
                {

                    if (property.PropertyType.ToString() == "System.Collections.Generic.IEnumerable`1[System.String]")
                    {
                        string[] value = property.GetValue(this) as string[];

                        if (value.Length == 0)
                            continue;

                        result += $"[{optAttr.LongName}]\n";
                        foreach (var item in value)
                        {
                            result += $"            {item}\n";
                        }
                    }
                    else
                    {
                        result += $"[{optAttr.LongName}]    {property.GetValue(this)}\n";
                    }
                }
            }
            return result;
        }

        public static CommandLineOptions Load(string localPath)
        {
            var options = CommandLineParser.GetCommandLineOptions<CommandLineOptions>();
            if (options != null && !options.Empty)
            {
                NcDebug.Log($"Get options from commandline.");
                return options;
            }

#if !UNITY_EDITOR && UNITY_ANDROID
            // error: current no clo.json
            UnityEngine.WWW www = new UnityEngine.WWW(Platform.GetStreamingAssetsPath("clo.json"));
            while (!www.isDone)
            {
                // wait for data load
            }
            return JsonSerializer.Deserialize<CommandLineOptions>(www.text, JsonOptions);
#else
            if (File.Exists(localPath))
            {
                NcDebug.Log($"Get options from local: {localPath}");
                return JsonSerializer.Deserialize<CommandLineOptions>(File.ReadAllText(localPath), JsonOptions);
            }
#endif

            NcDebug.LogErrorFormat("Failed to find {0}. Using default options.", localPath);
            return new CommandLineOptions();
        }

        public class StringEnumerableConverter : JsonConverter<IEnumerable<string>>
        {
            public override IEnumerable<string> Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("Expected a start of an array");
                }

                if (!reader.Read())
                {
                    throw new JsonException("Expected an end of an array or a string");
                }

                var result = new List<string>();
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    result.Add(reader.GetString());
                    if (!reader.Read())
                    {
                        throw new JsonException("Expected an end of an array or a string");
                    }
                }

                if (reader.TokenType != JsonTokenType.EndArray)
                {
                    throw new JsonException("Expected an end of an array");
                }

                return result;
            }

            public override void Write(
                Utf8JsonWriter writer,
                IEnumerable<string> value,
                JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                foreach (string el in value)
                {
                    writer.WriteStringValue(el);
                }
                writer.WriteEndArray();
            }
        }
    }
}
