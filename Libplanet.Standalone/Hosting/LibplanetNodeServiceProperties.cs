using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net;

namespace Libplanet.Standalone.Hosting
{
    public class LibplanetNodeServiceProperties<T>
        where T : IAction, new()
    {
        // swarm.
        public string Host { get; set; }

        public ushort? Port { get; set; }

        public PrivateKey PrivateKey { get; set; }

        public string StoreType { get; set; }

        public string StorePath { get; set; }

        public int StoreStatesCacheSize { get; set; }

        public string GenesisBlockPath { get; set; }

        public Block<T> GenesisBlock { get; set; }

        public IEnumerable<Peer> Peers { get; set; }

        public IImmutableSet<Address> TrustedStateValidators { get; set; }

        public bool NoMiner { get; set; }

        public IEnumerable<IceServer> IceServers { get; set; }

        public AppProtocolVersion AppProtocolVersion { get; set; }

        public ISet<PublicKey> TrustedAppProtocolVersionSigners { get; set; }

        public int MinimumDifficulty { get; set; }

        public DifferentAppProtocolVersionEncountered DifferentAppProtocolVersionEncountered { get; set; }

        public bool Render { get; set; }
    }
}
