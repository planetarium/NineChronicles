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

        public const string Postfix = "";  // E.g., "_20191211"

        public static IImmutableDictionary<Env, string> DirNames = new Dictionary<Env, string>
        {
            [Env.Production] = "9c",
            [Env.Development] = "9c_dev",
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

            string prefix =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dirname = DirNames.TryGetValue(env.Value, out string dirName) ? dirName : DirNames[Env.Production];
            // Linux/macOS: $HOME/.local/share/planetarium/9c
            // Windows: %LOCALAPPDATA%\planetarium\9c (i.e., %HOME%\AppData\Local\planetarium\9c)
            return Path.Combine(
                prefix,
                "planetarium",
                dirname + Postfix
            );
        }
    }
}
