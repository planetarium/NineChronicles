using CommandLine;
using CommandLine.Text;
using System;
using System.Linq;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class CommandLineParser
    {
        public static T GetCommandLineOptions<T>() where T : class
        {
            var args = Environment.GetCommandLineArgs();

            // args may contain raw private key string. like "--private-key={private-key}".
            // and, WE MUST DO NOT LOG PRIVATE KEY.
            var filteredArgs = args.Where(str =>
                !str.Contains("private")).ToList();
            var argsString = string.Join(" ", filteredArgs);
            NcDebug.Log($"[CommandLineParser] GetCommandLineOptions<{typeof(T).Name}> invoked" +
                      $" with {argsString}");

            var parser = new Parser(with => with.IgnoreUnknownArguments = true);
            ParserResult<T> result = parser.ParseArguments<T>(args);
            if (result.Tag == ParserResultType.Parsed)
            {
                return ((Parsed<T>)result).Value;
            }

            result.WithNotParsed(_ => NcDebug.Log(HelpText.AutoBuild(result)));
            return null;
        }
    }
}
