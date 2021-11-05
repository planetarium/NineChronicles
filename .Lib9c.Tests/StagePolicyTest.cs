namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lib9c.Tests.TestHelper;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Crypto;
    using Libplanet.Tx;
    using Nekoyume.Action;
    using Nekoyume.BlockChain;
    using Nekoyume.BlockChain.Policy;
    using Serilog.Core;
    using Xunit;
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

    public class StagePolicyTest
    {
        private readonly PrivateKey[] _accounts;

        private readonly Dictionary<Address, Transaction<NCAction>[]> _txs;

        // It's safe beause we don't use `BlockChain<T>` atm...
        private readonly BlockChain<NCAction> _chain = null;

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
            var policy = new StagePolicy(
                default,
                2
            );

            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][0]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][1]);
            policy.Stage(_chain, _txs[_accounts[1].ToAddress()][0]);
            policy.Stage(_chain, _txs[_accounts[2].ToAddress()][0]);

            AssertTxs(
                policy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1],
                _txs[_accounts[1].ToAddress()][0],
                _txs[_accounts[2].ToAddress()][0]
            );
        }

        [Fact]
        public void StageOverQuota()
        {
            var policy = new StagePolicy(
                default,
                2
            );

            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][0]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][1]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][2]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][3]);

            AssertTxs(
                policy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1]
            );
        }

        [Fact]
        public void StageOverQuotaInverseOrder()
        {
            var policy = new StagePolicy(
                default,
                2
            );

            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][3]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][2]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][1]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][0]);

            AssertTxs(
                policy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1]
            );
        }

        [Fact]
        public void StageOverQuotaOutOfOrder()
        {
            var policy = new StagePolicy(
                default,
                2
            );

            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][2]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][1]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][3]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][0]);

            AssertTxs(
                policy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1]
            );
        }

        [Fact]
        public void StageSameNonce()
        {
            var policy = new StagePolicy(
                default,
                2
            );
            var txA = Transaction<NCAction>.Create(0, _accounts[0], default, new NCAction[0]);
            var txB = Transaction<NCAction>.Create(0, _accounts[0], default, new NCAction[0]);
            var txC = Transaction<NCAction>.Create(0, _accounts[0], default, new NCAction[0]);

            policy.Stage(_chain, txA);
            policy.Stage(_chain, txB);
            policy.Stage(_chain, txC);

            AssertTxs(policy, txA, txB);
        }

        [Fact]
        public async Task StateFromMultiThread()
        {
            var policy = new StagePolicy(
                default,
                2
            );

            await Task.WhenAll(
                Enumerable
                    .Range(0, 40)
                    .Select(i => Task.Run(() =>
                    {
                        policy.Stage(_chain, _txs[_accounts[i / 10].ToAddress()][i % 10]);
                    }))
            );
            AssertTxs(
                policy,
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
            var policy = new StagePolicy(
                default,
                2
            );

            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][0]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][1]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][2]);
            policy.Stage(_chain, _txs[_accounts[0].ToAddress()][3]);

            AssertTxs(
                policy,
                _txs[_accounts[0].ToAddress()][0],
                _txs[_accounts[0].ToAddress()][1]
            );

            policy.Unstage(_chain, _txs[_accounts[0].ToAddress()][0].Id);

            AssertTxs(
                policy,
                _txs[_accounts[0].ToAddress()][1],
                _txs[_accounts[0].ToAddress()][2]
            );
        }

        [Fact]
        public void CalculateNextTxNonceCorrectWhenTxOverQuota()
        {
            var stagePolicy = new StagePolicy(
                default,
                2);
            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy();
            BlockChain<PolymorphicAction<ActionBase>> chain =
                BlockChainHelper.MakeBlockChain(new[] { blockPolicySource.BlockRenderer }, policy, stagePolicy);

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
                stagePolicy,
                txA,
                txB);
        }

        private void AssertTxs(StagePolicy policy, params Transaction<NCAction>[] txs)
        {
            foreach (Transaction<NCAction> tx in txs)
            {
                Assert.Equal(tx, policy.Get(_chain, tx.Id, false));
            }

            Assert.Equal(
                txs.ToHashSet(),
                policy.Iterate().ToHashSet()
            );
        }
    }
}
