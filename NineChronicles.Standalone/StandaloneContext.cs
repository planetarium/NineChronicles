using Libplanet.Blockchain;
using Libplanet.Crypto;

using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class StandaloneContext
    {
        public BlockChain<NineChroniclesActionType> BlockChain { get; set; }
        public PrivateKey PrivateKey { get; set; }
    }
}
