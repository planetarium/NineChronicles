using CommandLine;
using CommandLine.Text;
using System;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class CommandLineParser
    {
        public static T GetCommandLineOptions<T>() where T : class
        {
            var args = Environment.GetCommandLineArgs();
            var parser = new Parser(with => with.IgnoreUnknownArguments = true);
            ParserResult<T> result = parser.ParseArguments<T>(args);
            if (result.Tag == ParserResultType.Parsed)
            {
                return ((Parsed<T>)result).Value;
            }

            result.WithNotParsed(_ =>Debug.Log(HelpText.AutoBuild(result)));
            return null;
        }
    }
}
