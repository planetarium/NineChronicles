using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Libplanet;
using Serilog;
using Libplanet.KeyStore;
using Qml.Net;

namespace Launcher
{
    public class KeyStore
    {
        public KeyStore(string keyStorePath)
        {
            KeyStorePath = keyStorePath;
        }

        public string KeyStorePath { get; }

        public Dictionary<Address, ProtectedPrivateKey> ProtectedPrivateKeys
        {
            get {
                if (!Directory.Exists(KeyStorePath))
                {
                    Directory.CreateDirectory(KeyStorePath);
                }

                var keyPaths = Directory.EnumerateFiles(KeyStorePath);

                var protectedPrivateKeys = new Dictionary<Address, ProtectedPrivateKey>();
                foreach (var keyPath in keyPaths)
                {
                    if (Path.GetFileName(keyPath) is string f && f.StartsWith("."))
                    {
                        continue;
                    }

                    using (Stream stream = new FileStream(keyPath, FileMode.Open))
                    using (var reader = new StreamReader(stream))
                    {
                        try
                        {
                            var protectedPrivateKey = ProtectedPrivateKey.FromJson(reader.ReadToEnd());
                            protectedPrivateKeys[protectedPrivateKey.Address] = protectedPrivateKey;
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "The key file {keyPath} is invalid: {exception}", keyPath, e);
                        }
                    }
                }

                return protectedPrivateKeys;
            }
        }

        // Of course, it can be replaced with LINQ `Select`. But QML doesn't support it so exists.
        [NotifySignal]
        public List<string> Addresses =>
            ProtectedPrivateKeys.Keys.Select(key => key.ToHex()).ToList();
    }
}
