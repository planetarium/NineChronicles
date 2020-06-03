using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Nekoyume.Action;

namespace NineChronicles.Standalone
{
    public class StandaloneContext : IStandaloneContext
    {
        public BlockChain<PolymorphicAction<ActionBase>> BlockChain { get; set; }
        public PrivateKey PrivateKey { get; set; }
    }
}
