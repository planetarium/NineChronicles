using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.Helper
{
    public class CommandLineOptions
    {
        [Option("private-key", Required = false, HelpText = "The private key to use.")]
        public string PrivateKey { get; set; }

        [Option("host", Required = false, HelpText = "The host name to use.")]
        public string Host { get; set; }

        [Option("port", Required = false, HelpText = "The source port to use.")]
        public int? Port { get; set; }

        [Option("no-miner", Required = false, HelpText = "Do not mine block.")]
        public bool NoMiner { get; set; }

        [Option("peer", Required = false, HelpText = "Peers to add. (Usage: --peer peerA peerB ...)")]
        public IEnumerable<string> Peers { get; set; }
    }

    public static class CommnadLineParser
    {
        public static CommandLineOptions GetCommandLineOptions()
        {
            string[] args = Environment.GetCommandLineArgs().Where(s => s.StartsWith("--")).ToArray();
            ParserResult<CommandLineOptions> result = Parser.Default.ParseArguments<CommandLineOptions>(args);

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
