using Libplanet.Blockchain;
using Libplanet.Crypto;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public interface IStandaloneContext
    {
        BlockChain<NineChroniclesActionType> BlockChain { get; set; }

        PrivateKey PrivateKey { get; set; }
    }
}
