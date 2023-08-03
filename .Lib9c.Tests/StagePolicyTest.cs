namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lib9c.Tests.TestHelper;
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.Types.Tx;
    using Nekoyume.Action;
    using Nekoyume.Blockchain;
    using Nekoyume.Blockchain.Policy;
    using Serilog.Core;
    using Xunit;

    public class StagePolicyTest
    {
        private readonly PrivateKey[] _accounts;

        private readonly Dictionary<Address, Transaction[]> _txs;

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
                        n => Transaction.Create(
                            n,
                            acc,
                            default,
                            new ActionBase[0].ToPlainValues()
                        )
                    )
                    .ToArray()
            );
        }

        [Fact]
        public void Stage()
        {
            NCStagePolicy stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain chain = MakeChainWithStagePolicy(stagePolicy);

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
            NCStagePolicy stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain chain = MakeChainWithStagePolicy(stagePolicy);

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
            NCStagePolicy stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain chain = MakeChainWithStagePolicy(stagePolicy);

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
            NCStagePolicy stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain chain = MakeChainWithStagePolicy(stagePolicy);

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
            NCStagePolicy stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain chain = MakeChainWithStagePolicy(stagePolicy);
            var txA = Transaction.Create(0, _accounts[0], default, new ActionBase[0].ToPlainValues());
            var txB = Transaction.Create(0, _accounts[0], default, new ActionBase[0].ToPlainValues());
            var txC = Transaction.Create(0, _accounts[0], default, new ActionBase[0].ToPlainValues());

            stagePolicy.Stage(chain, txA);
            stagePolicy.Stage(chain, txB);
            stagePolicy.Stage(chain, txC);

            AssertTxs(chain, stagePolicy, txA, txB);
        }

        [Fact]
        public async Task StateFromMultiThread()
        {
            NCStagePolicy stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain chain = MakeChainWithStagePolicy(stagePolicy);

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
            NCStagePolicy stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain chain = MakeChainWithStagePolicy(stagePolicy);

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
            NCStagePolicy stagePolicy = new NCStagePolicy(TimeSpan.FromHours(1), 2);
            BlockChain chain = MakeChainWithStagePolicy(stagePolicy);

            long nextTxNonce = chain.GetNextTxNonce(_accounts[0].ToAddress());
            Assert.Equal(0, nextTxNonce);
            var txA = Transaction.Create(nextTxNonce, _accounts[0], default, new ActionBase[0].ToPlainValues());
            stagePolicy.Stage(chain, txA);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].ToAddress());
            Assert.Equal(1, nextTxNonce);
            var txB = Transaction.Create(nextTxNonce, _accounts[0], default, new ActionBase[0].ToPlainValues());
            stagePolicy.Stage(chain, txB);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].ToAddress());
            Assert.Equal(2, nextTxNonce);
            var txC = Transaction.Create(nextTxNonce, _accounts[0], default, new ActionBase[0].ToPlainValues());
            stagePolicy.Stage(chain, txC);

            nextTxNonce = chain.GetNextTxNonce(_accounts[0].ToAddress());
            Assert.Equal(3, nextTxNonce);

            AssertTxs(
                chain,
                stagePolicy,
                txA,
                txB);
        }

        private void AssertTxs(BlockChain blockChain, NCStagePolicy policy, params Transaction[] txs)
        {
            foreach (Transaction tx in txs)
            {
                Assert.Equal(tx, policy.Get(blockChain, tx.Id, filtered: true));
            }

            Assert.Equal(
                txs.ToHashSet(),
                policy.Iterate(blockChain, filtered: true).ToHashSet()
            );
        }

        private BlockChain MakeChainWithStagePolicy(NCStagePolicy stagePolicy)
        {
            BlockPolicySource blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy policy = blockPolicySource.GetPolicy();
            BlockChain chain =
                BlockChainHelper.MakeBlockChain(
                    blockRenderers: new[] { blockPolicySource.BlockRenderer },
                    policy: policy,
                    stagePolicy: stagePolicy);
            return chain;
        }
    }
}
