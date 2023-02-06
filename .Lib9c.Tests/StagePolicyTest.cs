namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lib9c.Tests.TestHelper;
    using Libplanet;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.Tx;
    using Nekoyume.BlockChain;
    using Nekoyume.BlockChain.Policy;
    using Serilog.Core;
    using Xunit;
#pragma warning disable SA1135
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
#pragma warning restore SA1135

    public class StagePolicyTest
    {
        private readonly PrivateKey[] _accounts;

        private readonly Dictionary<Address, Transaction<NCAction>[]> _txs;

        public StagePolicyTest()
        {
            _accounts = new[]
            {
                new PrivateKey(),
                new PrivateKey(),
                new PrivateKey(),
                new PrivateKey(),
            };
            _txs = _accounts.ToDictionary(
                acc => acc.ToAddress(),
                acc => Enumerable
                    .Range(0, 10)
                    .Select(
                        n => Transaction<NCAction>.Create(
                            n,
                            acc,
                            default,
                            new NCAction[0]
                        )
                    )
                    .ToArray()
            );
        }

        [Fact]
        public void Stage()
        {
            StagePolicy stagePolicy = new StagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain<NCAction> chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][0]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][1]);
            stagePolicy.Stage(chain, _txs[_accounts[1].ToAddress()][0]);
            stagePolicy.Stage(chain, _txs[_accounts[2].ToAddress()][0]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1],
                _txs[_accounts[1].ToAddress()][0],
                _txs[_accounts[2].ToAddress()][0]
            );
        }

        [Fact]
        public void StageOverQuota()
        {
            StagePolicy stagePolicy = new StagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain<NCAction> chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][0]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][1]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][2]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][3]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1]
            );
        }

        [Fact]
        public void StageOverQuotaInverseOrder()
        {
            StagePolicy stagePolicy = new StagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain<NCAction> chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][3]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][2]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][1]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][0]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1]
            );
        }

        [Fact]
        public void StageOverQuotaOutOfOrder()
        {
            StagePolicy stagePolicy = new StagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain<NCAction> chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][2]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][1]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][3]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][0]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1]
            );
        }

        [Fact]
        public void StageSameNonce()
        {
            StagePolicy stagePolicy = new StagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain<NCAction> chain = MakeChainWithStagePolicy(stagePolicy);
            var txA = Transaction<NCAction>.Create(0, _accounts[0], default, new NCAction[0]);
            var txB = Transaction<NCAction>.Create(0, _accounts[0], default, new NCAction[0]);
            var txC = Transaction<NCAction>.Create(0, _accounts[0], default, new NCAction[0]);

            stagePolicy.Stage(chain, txA);
            stagePolicy.Stage(chain, txB);
            stagePolicy.Stage(chain, txC);

            AssertTxs(chain, stagePolicy, txA, txB);
        }

        [Fact]
        public async Task StateFromMultiThread()
        {
            StagePolicy stagePolicy = new StagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain<NCAction> chain = MakeChainWithStagePolicy(stagePolicy);

            await Task.WhenAll(
                Enumerable
                    .Range(0, 40)
                    .Select(i => Task.Run(() =>
                    {
                        stagePolicy.Stage(chain, _txs[_accounts[i / 10].ToAddress()][i % 10]);
                    }))
            );
            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1],
                _txs[_accounts[1].ToAddress()][0],
                _txs[_accounts[1].ToAddress()][1],
                _txs[_accounts[2].ToAddress()][0],
                _txs[_accounts[2].ToAddress()][1],
                _txs[_accounts[3].ToAddress()][0],
                _txs[_accounts[3].ToAddress()][1]
            );
        }

        [Fact]
        public void IterateAfterUnstage()
        {
            StagePolicy stagePolicy = new StagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain<NCAction> chain = MakeChainWithStagePolicy(stagePolicy);

            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][0]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][1]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][2]);
            stagePolicy.Stage(chain, _txs[_accounts[0].ToAddress()][3]);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1]
            );

            stagePolicy.Unstage(chain, _txs[_accounts[0].ToAddress()][0].Id);

            AssertTxs(
                chain,
                stagePolicy,
                _txs[_accounts[0].ToAddress()][1],
                _txs[_accounts[0].ToAddress()][2]
            );
        }

        [Fact]
        public void CalculateNextTxNonceCorrectWhenTxOverQuota()
        {
            StagePolicy stagePolicy = new StagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain<NCAction> chain = MakeChainWithStagePolicy(stagePolicy);

            long nextTxNonce = chain.GetNextTxNonce(_accounts[0].ToAddress());
            Assert.Equal(0, nextTxNonce);
            var txA = Transaction<NCAction>.Create(nextTxNonce, _accounts[0], default, new NCAction[0]);
            stagePolicy.Stage(chain, txA);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].ToAddress());
            Assert.Equal(1, nextTxNonce);
            var txB = Transaction<NCAction>.Create(nextTxNonce, _accounts[0], default, new NCAction[0]);
            stagePolicy.Stage(chain, txB);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].ToAddress());
            Assert.Equal(2, nextTxNonce);
            var txC = Transaction<NCAction>.Create(nextTxNonce, _accounts[0], default, new NCAction[0]);
            stagePolicy.Stage(chain, txC);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].ToAddress());
            Assert.Equal(3, nextTxNonce);

            AssertTxs(
                chain,
                stagePolicy,
                txA,
                txB);
        }

        private void AssertTxs(BlockChain<NCAction> blockChain, StagePolicy policy, params Transaction<NCAction>[] txs)
        {
            foreach (Transaction<NCAction> tx in txs)
            {
                Assert.Equal(tx, policy.Get(blockChain, tx.Id, filtered: true));
            }

            Assert.Equal(
                txs.ToHashSet(),
                policy.Iterate(blockChain, filtered: true).ToHashSet()
            );
        }

        private BlockChain<NCAction> MakeChainWithStagePolicy(StagePolicy stagePolicy)
        {
            BlockPolicySource blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<NCAction> policy = blockPolicySource.GetPolicy();
            BlockChain<NCAction> chain =
                BlockChainHelper.MakeBlockChain(
                    blockRenderers: new[] { blockPolicySource.BlockRenderer },
                    policy: policy,
                    stagePolicy: stagePolicy);
            return chain;
        }
    }
}
