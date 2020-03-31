using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        private string storageType;

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

        // Unity 단독 빌드시의 해시 파워가 낮기 때문에, Unity 버전의 기존치는 .NET Core보다 낮게 잡습니다.
        private int minimumDifficulty = 100000;

        public bool Empty { get; private set; } = true;

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

        [Option('D', "minimum-difficulty", Required = false)]
        public int MinimumDifficulty
        {
            get => minimumDifficulty;
            set
            {
                minimumDifficulty = value;
                Empty = false;
            }
        }

        public static CommandLineOptions Load(string localPath, string onlinePath)
        {
            var options = CommnadLineParser.GetCommandLineOptions();
            if (!options.Empty)
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

            try
            {
                var webResponse = WebRequest.Create(onlinePath).GetResponse();
                using (var stream = webResponse.GetResponseStream())
                {
                    if (!(stream is null))
                    {
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        string jsonData = Encoding.UTF8.GetString(data);
                        Debug.Log($"Get options from web: {onlinePath}");
                        return JsonSerializer.Deserialize<CommandLineOptions>(jsonData, jsonOptions);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

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

    public static class CommnadLineParser
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
