using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using System;

namespace Nekoyume.Blockchain.Policy
{
    public class NCBlockPolicy : BlockPolicy
    {
        public NCBlockPolicy(
            IAction blockAction,
            TimeSpan blockInterval,
            Func<BlockChain, Transaction, TxPolicyViolationException>
                validateNextBlockTx = null,
            Func<BlockChain, Block, BlockPolicyViolationException>
                validateNextBlock = null,
            Func<long, long> getMaxTransactionsBytes = null,
            Func<long, int> getMinTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerBlock = null,
            Func<long, int> getMaxTransactionsPerSignerPerBlock = null)
            : base(
                blockAction: blockAction,
                blockInterval: blockInterval,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxTransactionsBytes: getMaxTransactionsBytes,
                getMinTransactionsPerBlock: getMinTransactionsPerBlock,
                getMaxTransactionsPerBlock: getMaxTransactionsPerBlock,
                getMaxTransactionsPerSignerPerBlock: getMaxTransactionsPerSignerPerBlock)
        {
        }
    }
}
