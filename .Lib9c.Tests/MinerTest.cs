namespace Lib9c.Tests
{
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

    public class MinerTest
    {
        [Fact]
        public async Task Proof()
        {
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            var blockPolicySource = new BlockPolicySource(Logger.None);
            var genesis = BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(HashAlgorithmType.Of<SHA256>());
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                blockPolicySource.GetPolicy(
                    minimumDifficulty: 50_000,
                    maximumTransactions: 100
                ),
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>(),
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            var minerKey = new PrivateKey();
            var miner = new Miner(blockChain, null, minerKey, false);
            Block<PolymorphicAction<ActionBase>> mined = await miner.MineBlockAsync(100, default);
            Transaction<PolymorphicAction<ActionBase>> tx = Assert.Single(mined.Transactions);

            Assert.Equal(miner.Address, tx.Signer);
        }
    }
}
