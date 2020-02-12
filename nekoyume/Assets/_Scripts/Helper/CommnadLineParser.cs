using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;

namespace Nekoyume.Helper
{
    [Serializable]
    public class CommandLineOptions
    {
        // JSON 직렬화를 위해 필드와 속성을 둘 다 기술합니다.
        public string privateKey;

        public string keyStorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "planetarium",
            "keystore"
        );
        // Linux/macOS: $HOME/.config/planetarium/keystore
        // Windows: %USERPROFILE%\AppData\Roaming\planetarium\keystore

        public string host;

        public int port;

        public bool noMiner;

        public string[] peers = new string[]{ };

        public string[] iceServers = new string[]{ };

        public string storagePath;

        public bool autoPlay;

        public bool consoleSink;

        public bool development;

        public bool maintenance;

        public bool testEnd;

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

        public static CommandLineOptions Load(string localPath, string onlinePath)
        {
            var options = CommnadLineParser.GetCommandLineOptions();
            if (!options.Empty)
            {
                Debug.Log($"Get options from commandline.");
                return options;
            }

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
                        return JsonUtility.FromJson<CommandLineOptions>(jsonData);
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
                return JsonUtility.FromJson<CommandLineOptions>(File.ReadAllText(localPath));
            }

            Debug.Log("Failed to find options. Creating...");
            return new CommandLineOptions();
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
