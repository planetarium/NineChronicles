using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Action;

namespace Nekoyume.Blockchain.Policy
{
    public class DebugPolicy : IBlockPolicy
    {
        public DebugPolicy()
        {
        }

        public IAction BlockAction { get; } = new RewardGold();

        public TxPolicyViolationException ValidateNextBlockTx(
            BlockChain blockChain, Transaction transaction)
        {
            return null;
        }

        public BlockPolicyViolationException ValidateNextBlock(
            BlockChain blockChain, Block nextBlock)
        {
            return null;
        }

        public long GetMaxTransactionsBytes(long index) => long.MaxValue;

        public int GetMinTransactionsPerBlock(long index) => 0;

        public int GetMaxTransactionsPerBlock(long index) => int.MaxValue;

        public int GetMaxTransactionsPerSignerPerBlock(long index) => int.MaxValue;

        public int GetMinBlockProtocolVersion(long index) => 0;
    }
}
