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
        private  string dccAvatars;
        public  bool Empty { get; private set; } = true;

        [Option("dcc-avatars", Required = false, HelpText = "dcc avatar list")]
        public  string DccAvatars
        {
            get => dccAvatars;
            set
            {
                dccAvatars = value;
                Empty = false;
            }
        }

        public static Url Load(string localPath)
        {
            var options = CommandLineParser.GetCommandLineOptions<Url>();
            if (options is { Empty: false })
            {
                Debug.Log($"Get options from commandline.");
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

            if (File.Exists(localPath))
            {
                Debug.Log($"Get url from local: {localPath}");
                return JsonSerializer.Deserialize<Url>(File.ReadAllText(localPath), jsonOptions);
            }

            Debug.LogErrorFormat("Failed to find {0}. Using default url.", localPath);
            return new Url();
        }
    }
}
