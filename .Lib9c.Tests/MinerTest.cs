namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Tx;
    using Nekoyume.Action;
    using Nekoyume.BlockChain;
    using Serilog.Core;
    using Xunit;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public class MinerTest
    {
        [Fact]
        public async Task Proof()
        {
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            var blockPolicySource = new BlockPolicySource(Logger.None);
            var genesis = BlockChain<NCAction>.MakeGenesisBlock(HashAlgorithmType.Of<SHA256>());
            var blockChain = new BlockChain<NCAction>(
                blockPolicySource.GetPolicy(
                    minimumDifficulty: 50_000,
                    maximumTransactions: 100
                ),
                new VolatileStagePolicy<NCAction>(),
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            var minerKey = new PrivateKey();
            var miner = new Miner(blockChain, null, minerKey, false);
            Block<NCAction> mined = await miner.MineBlockAsync(100, default);
            Transaction<NCAction> tx = Assert.Single(mined.Transactions);

            Assert.Equal(miner.Address, tx.Signer);
        }

        [Fact]
        public void GetProofTxPriority()
        {
            BlockHash genesis = default(BlockHash);
            NCAction[] actions = new NCAction[0];
            Transaction<NCAction>[] txs = Enumerable.Range(0, 100)
                .Select(_ => Transaction<NCAction>.Create(0, new PrivateKey(), genesis, actions))
                .ToArray();

            for (int i = 0; i < 100; i += 5)
            {
                Transaction<NCAction> proof = txs[i];
                IComparer<Transaction<NCAction>> txPriority = Miner.GetProofTxPriority(proof);
                Assert.Same(proof, txs.OrderBy(tx => tx, txPriority).First());
            }
        }
    }
}
