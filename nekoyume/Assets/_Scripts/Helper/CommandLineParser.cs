using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nekoyume.L10n;
using UnityEngine;

namespace Nekoyume.Helper
{
    [Serializable]
    public class CommandLineOptions
    {
        private string privateKey;

        // null이면 Web3KeyStore.DefaultStore 따름
        private string keyStorePath;

        private string host;

        private int port;

        private bool noMiner;

        private string[] peers = new string[] { };

        private string[] iceServers = new string[] { };

        private string storagePath;

        private string storageType = "rocksdb";

        private bool rpcClient;

        private string rpcServerHost;

        private int rpcServerPort;

        private bool autoPlay;

        private bool consoleSink;

        private bool development;

        private bool maintenance;

        private bool testEnd;

        private string appProtocolVersion;

        private string[] trustedAppProtocolVersionSigners = new string[] { };

        private string language = "English";

        private string awsSinkGuid;

        private string apiServerHost;

        private string onBoardingHost;

        private double sentrySampleRate = 0;

        public bool Empty { get; private set; } = true;

        public string genesisBlockPath;

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

        public static CommandLineOptions Load(string localPath)
        {
            var options = CommandLineParser.GetCommandLineOptions();
            if (options != null && !options.Empty)
            {
                Debug.Log($"Get options from commandline.");
                return options;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                Converters =
                {
                    new StringEnumerableConverter(),
                },
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };

            if (File.Exists(localPath))
            {
                Debug.Log($"Get options from local: {localPath}");
                return JsonSerializer.Deserialize<CommandLineOptions>(File.ReadAllText(localPath), jsonOptions);
            }

            Debug.LogErrorFormat("Failed to find {0}. Using default options.", localPath);
            return new CommandLineOptions();
        }

        private class StringEnumerableConverter : JsonConverter<IEnumerable<string>>
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

    public static class CommandLineParser
    {
        public static CommandLineOptions GetCommandLineOptions()
        {
            string[] args = Environment.GetCommandLineArgs();

            var parser = new Parser(with => with.IgnoreUnknownArguments = true);

            ParserResult<CommandLineOptions> result = parser.ParseArguments<CommandLineOptions>(args);

            if (result.Tag == ParserResultType.Parsed)
            {
                return ((Parsed<CommandLineOptions>)result).Value;
            }

            result.WithNotParsed(
                errors =>
                    Debug.Log(HelpText.AutoBuild(result)
            ));

            return null;
        }
    }
}
