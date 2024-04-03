using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Nekoyume.Helper
{
    public class Url
    {
        private string dccAvatars;
        private string dccMetadata;
        private string dccConnect;
        private string dccMileageAPI;
        private string dccMileageShop;
        private string dccEthChainHeaderName;
        private string dccEthChainHeaderValue;
        public bool Empty { get; private set; } = true;

        [Option("dcc-avatars", Required = true, HelpText = "dcc avatar list")]
        public string DccAvatars
        {
            get => dccAvatars;
            set
            {
                dccAvatars = value;
                Empty = false;
            }
        }

        [Option("dcc-metadata", Required = true, HelpText = "dcc metadata")]
        public string DccMetadata
        {
            get => dccMetadata;
            set
            {
                dccMetadata = value;
                Empty = false;
            }
        }

        [Option("dcc-metadata", Required = true, HelpText = "dcc connect")]
        public string DccConnect
        {
            get => dccConnect;
            set
            {
                dccConnect = value;
                Empty = false;
            }
        }

        [Option("dcc-mileage-shop", Required = true, HelpText = "dcc mileage shop")]
        public string DccMileageShop
        {
            get => dccMileageShop;
            set
            {
                dccMileageShop = value;
                Empty = false;
            }
        }

        [Option("dcc-open-api", Required = true, HelpText = "dcc open api")]
        public string DccMileageAPI
        {
            get => dccMileageAPI;
            set
            {
                dccMileageAPI = value;
                Empty = false;
            }
        }

        [Option("dcc-eth-chain-header-name", Required = true, HelpText = "Dcc eth chain header name")]
        public string DccEthChainHeaderName
        {
            get => dccEthChainHeaderName;
            set
            {
                dccEthChainHeaderName = value;
                Empty = false;
            }
        }

        [Option("dcc-eth-chain-header-value", Required = true, HelpText = "Dcc eth chain header value")]
        public string DccEthChainHeaderValue
        {
            get => dccEthChainHeaderValue;
            set
            {
                dccEthChainHeaderValue = value;
                Empty = false;
            }
        }

        public static Url Load(string localPath)
        {
            var options = CommandLineParser.GetCommandLineOptions<Url>();
            if (options is { Empty: false })
            {
                NcDebug.Log($"Get options from commandline.");
                return options;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                Converters =
                {
                    new CommandLineOptions.StringEnumerableConverter(),
                },
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
#if !UNITY_EDITOR && (UNITY_ANDROID)
            UnityEngine.WWW www = new UnityEngine.WWW(localPath);
            while (!www.isDone)
            {
                // wait for data load
            }
            return JsonSerializer.Deserialize<Url>(www.text, jsonOptions);
#endif
            try
            {
                NcDebug.Log($"Get url from local: {localPath}");
                var jsonText = File.ReadAllText(localPath);
                NcDebug.Log($"loaded plain json: {jsonText}");
                return JsonSerializer.Deserialize<Url>(jsonText, jsonOptions);
            }
            catch(Exception e)
            {
                NcDebug.LogErrorFormat("Failed to find {0}. Using default url.\nException: {1}", localPath, e);
                return new Url();
            }
        }
    }
}
