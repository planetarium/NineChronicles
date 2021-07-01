using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nekoyume.Game.Util
{
    public static class EnumGenerator
    {
        private const string Path = "Assets/_Scripts/EnumType/";

        public static void Generate<T>(T enumType, IEnumerable<string> enums) where T : Enum
        {
            var nameSpace = enumType.GetType().Namespace;
            var enumName = enumType.GetType().ToString();
            enumName = enumName.Replace($"{nameSpace}.", string.Empty);
            var filePathAndName = $"{Path}{enumName}.cs";
            using StreamWriter streamWriter = new StreamWriter(filePathAndName);
            streamWriter.WriteLine($"namespace {nameSpace}\n{{\n    public enum {enumName}\n    {{");
            foreach (var i in enums)
            {
                streamWriter.WriteLine($"\t{i}," );
            }
            streamWriter.WriteLine("    }\n}");
        }

        public static List<string> EnumToList<T>(T enumType) where T : Enum
        {
            return (from object i in Enum.GetValues(enumType.GetType()) select i.ToString()).ToList();
        }
    }
}
