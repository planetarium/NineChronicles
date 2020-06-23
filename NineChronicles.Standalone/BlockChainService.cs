using System.Numerics;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Net;
using Libplanet.Tx;
using MagicOnion;
using MagicOnion.Server;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Shared.Services;

using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone
{
    public class BlockChainService : ServiceBase<IBlockChainService>, IBlockChainService
    {
        private BlockChain<NineChroniclesActionType> _blockChain;
        private Swarm<NineChroniclesActionType> _swarm;
        private Codec _codec;

        public BlockChainService(
            BlockChain<NineChroniclesActionType> blockChain,
            Swarm<NineChroniclesActionType> swarm
        )
        {
            _blockChain = blockChain;
            _swarm = swarm;
            _codec = new Codec();
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
            // FIXME: Null과 null 구분해서 반환해야 할 듯
            byte[] encoded = _codec.Encode(state ?? new Null());
            return UnaryResult(encoded);
        }

        public UnaryResult<byte[]> GetBalance(byte[] addressBytes, byte[] currencyBytes)
        {
            var address = new Address(addressBytes);
            var serializedCurrency = (Bencodex.Types.Dictionary)_codec.Decode(currencyBytes);
            Currency currency = CurrencyExtensions.Deserialize(serializedCurrency);
            BigInteger balance = _blockChain.GetBalance(address, currency);
            byte[] encoded = _codec.Encode((Bencodex.Types.Integer)balance);
            return UnaryResult(encoded);
        }

        public UnaryResult<long> GetNextTxNonce(byte[] addressBytes)
        {
            var address = new Address(addressBytes);
            return UnaryResult(_blockChain.GetNextTxNonce(address));
        }
    }
}
