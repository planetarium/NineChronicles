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

        public Dictionary<Address, ProtectedPrivateKey> ProtectedPrivateKeys =>
            Nekoyume.KeyStore.GetProtectedPrivateKeys(KeyStorePath)
                .ToDictionary(pair => pair.Value.Address, pair => pair.Value);

        // Of course, it can be replaced with LINQ `Select`. But QML doesn't support it so exists.
        [NotifySignal]
        public List<string> Addresses =>
            ProtectedPrivateKeys.Keys.Select(key => key.ToHex()).ToList();
    }
}
