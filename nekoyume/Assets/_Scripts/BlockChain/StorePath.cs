using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Nekoyume.BlockChain
{
    public static class StorePath
    {
        public enum Env
        {
            Production,
            Development,
        }

        private const string Postfix = "";  // E.g., "_20191211"

        private static readonly IImmutableDictionary<Env, string> DirNames = new Dictionary<Env, string>
        {
#if LIB9C_DEV_EXTENSIONS
            [Env.Production] = "9c_qa",
            [Env.Development] = "9c_dev_qa",
#else
            [Env.Production] = "9c",
            [Env.Development] = "9c_dev",
#endif
        }.ToImmutableDictionary();

        public static string GetDefaultStoragePath(Env? env = null)
        {
            if (env is null)
            {
                env = Env.Production;
#if UNITY_EDITOR
                env = Env.Development;
#endif
            }

            var dirname = DirNames.TryGetValue(env.Value, out var dirName) ? dirName : DirNames[Env.Production];
            // Linux/macOS: $HOME/.local/share/planetarium/
            // Windows: %LOCALAPPDATA%\planetarium\ (i.e., %HOME%\AppData\Local\planetarium\)
            return Path.Combine(
                GetPrefixPath(),
                dirname + Postfix
            );

        }

        public static string GetPrefixPath()
        {
            var prefix = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // Linux/macOS: $HOME/.local/share/planetarium/
            // Windows: %LOCALAPPDATA%\planetarium\ (i.e., %HOME%\AppData\Local\planetarium\9c)
            return Path.Combine(prefix, "planetarium");
        }
    }
}
