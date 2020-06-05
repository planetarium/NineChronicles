using Libplanet.Blockchain;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class StandaloneContext
    {
        public BlockChain<NineChroniclesActionType> BlockChain { get; set; }
        public IKeyStore KeyStore { get; set; }
    }
}
