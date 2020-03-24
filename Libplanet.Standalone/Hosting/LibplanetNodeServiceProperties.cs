using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Net;

namespace Libplanet.Standalone.Hosting
{
    public struct LibplanetNodeServiceProperties
    {
        // swarm.
        public string Host { get; set; }

        public ushort? Port { get; set; }

        public PrivateKey PrivateKey { get; set; }

        public string StoreType { get; set; }

        public string StorePath { get; set; }

        public string GenesisBlockPath { get; set; }

        public IEnumerable<Peer> Peers { get; set; }

        public bool NoMiner { get; set; }

        public IEnumerable<IceServer> IceServers { get; set; }

        public AppProtocolVersion AppProtocolVersion { get; set; }

        public ISet<PublicKey> TrustedAppProtocolVersionSigners { get; set; }

        public int MinimumDifficulty { get; set; }
    }
}
