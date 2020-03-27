using System.Collections.Generic;
using System.Linq;
using Libplanet;
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
            Web3KeyStore.DefaultKeyStore.List()
                .ToDictionary(pair => pair.Item2.Address, pair => pair.Item2);

        // Of course, it can be replaced with LINQ `Select`. But QML doesn't support it so exists.
        [NotifySignal]
        public List<string> Addresses =>
            ProtectedPrivateKeys.Keys.Select(key => key.ToHex()).ToList();
    }
}
