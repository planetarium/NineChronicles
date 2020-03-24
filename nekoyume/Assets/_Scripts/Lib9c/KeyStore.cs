using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Libplanet.KeyStore;
using Serilog;

namespace Nekoyume
{
    public static class KeyStore
    {
        public static Dictionary<string, ProtectedPrivateKey> GetProtectedPrivateKeys(string keyStorePath)
        {
            if (!Directory.Exists(keyStorePath))
            {
                Directory.CreateDirectory(keyStorePath);
            }

            var keyPaths = Directory.EnumerateFiles(keyStorePath);

            var protectedPrivateKeys = new Dictionary<string, ProtectedPrivateKey>();
            foreach (var keyPath in keyPaths)
            {
                if (Path.GetFileName(keyPath) is string f && f.StartsWith("."))
                {
                    continue;
                }

                using (var reader = new StreamReader(keyPath))
                {
                    try
                    {
                        protectedPrivateKeys[keyPath] = ProtectedPrivateKey.FromJson(reader.ReadToEnd());
                    }
                    catch (Exception e)
                    {
                        Log.Warning("The key file {0} is invalid: {1}", keyPath, e);
                    }
                }
            }

            Log.Debug(
                "Loaded {0} protected keys in the keystore:\n{1}",
                protectedPrivateKeys.Count,
                string.Join("\n", protectedPrivateKeys.Select(kv => $"- {kv.Value}: {kv.Key}"))
            );

            return protectedPrivateKeys;
        }
    }
}
