using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Net;
using Libplanet.Tx;
using MagicOnion;
using MagicOnion.Server;
using Nekoyume.Action;
using Nekoyume.Shared.Services;

using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class BlockChainService : ServiceBase<IBlockChainService>, IBlockChainService
    {
        private BlockChain<NineChroniclesActionType> _blockChain;
        private Swarm<NineChroniclesActionType> _swarm;

        public BlockChainService(
            BlockChain<NineChroniclesActionType> blockChain,
            Swarm<NineChroniclesActionType> swarm
        )
        {
            _blockChain = blockChain;
            _swarm = swarm;
        }

        public UnaryResult<bool> PutTransaction(byte[] txBytes)
        {
            Transaction<PolymorphicAction<ActionBase>> tx =
                Transaction<PolymorphicAction<ActionBase>>.Deserialize(txBytes);

            try
            {
                tx.Validate();
                _blockChain.StageTransaction(tx);
                _swarm.BroadcastTxs(new[] { tx });

                return UnaryResult(true);
            }
            catch (InvalidTxException)
            {
                return UnaryResult(false);
            }
        }

        public UnaryResult<byte[]> GetState(byte[] addressBytes)
        {
            var address = new Address(addressBytes);
            IValue state = _blockChain.GetState(address);
            byte[] encoded = new Codec().Encode(state ?? new Null());
            return UnaryResult(encoded);
        }

        public UnaryResult<long> GetNextTxNonce(byte[] addressBytes)
        {
            var address = new Address(addressBytes);
            return UnaryResult(_blockChain.GetNextTxNonce(address));
        }
    }
}
