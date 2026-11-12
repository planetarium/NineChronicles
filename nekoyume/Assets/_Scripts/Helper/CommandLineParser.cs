using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class CommandLineParser
    {
        public static T GetCommandLineOptions<T>() where T : class
        {
            var args = Environment.GetCommandLineArgs();

            // args may contain raw private key string. like "--private-key={private-key}" or
            // "--private-key {private-key}". and, WE MUST DO NOT LOG PRIVATE KEY.
            var argsString = string.Join(" ", FilterSensitiveArgs(args));
            NcDebug.Log($"[CommandLineParser] GetCommandLineOptions<{typeof(T).Name}> invoked" +
                $" with {argsString}");

            var parser = new Parser(with => with.IgnoreUnknownArguments = true);
            var result = parser.ParseArguments<T>(args);
            if (result.Tag == ParserResultType.Parsed)
            {
                return ((Parsed<T>)result).Value;
            }

            result.WithNotParsed(_ => NcDebug.Log(HelpText.AutoBuild(result)));
            return null;
        }

        /// <summary>
        /// Returns <paramref name="args"/> without the arguments that may contain a private key,
        /// so that the result is safe to log. An argument that mentions "private" is removed,
        /// and when it is an option whose value is passed as the next argument
        /// (e.g. "--private-key {private-key}"), the value is removed too.
        /// </summary>
        internal static List<string> FilterSensitiveArgs(IReadOnlyList<string> args)
        {
            var filteredArgs = new List<string>();
            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                if (arg.IndexOf("private", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    filteredArgs.Add(arg);
                    continue;
                }

                var hasSeparateValue = arg.StartsWith("-") && !arg.Contains("=");
                if (hasSeparateValue && i + 1 < args.Count && !args[i + 1].StartsWith("-"))
                {
                    i++;
                }
            }

            return filteredArgs;
        }
    }
}
