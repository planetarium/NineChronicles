namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Tx;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.BlockChain;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Serilog.Core;
    using Xunit;

    public class BlockPolicyTest
    {
        private readonly PrivateKey _privateKey;
        private readonly Currency _currency;

        public BlockPolicyTest()
        {
            _privateKey = new PrivateKey();
            _currency = new Currency("NCG", 2, minter: _privateKey.ToAddress());
        }

        [Fact]
        public async Task DoesTransactionFollowsPolicy()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(10000, 100);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress)
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );
            Transaction<PolymorphicAction<ActionBase>> txByStranger =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    new PrivateKey(),
                    genesis.Hash,
                    new PolymorphicAction<ActionBase>[] { }
                );

            // New private key which is not in activated addresses list is blocked.
            Assert.False(policy.DoesTransactionFollowsPolicy(txByStranger, blockChain));

            var newActivatedPrivateKey = new PrivateKey();
            var newActivatedAddress = newActivatedPrivateKey.ToAddress();

            // Activate with admin account.
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new AddActivatedAccount(newActivatedAddress) }
            );
            await blockChain.MineBlock(adminAddress);

            Transaction<PolymorphicAction<ActionBase>> txByNewActivated =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    new PolymorphicAction<ActionBase>[] { }
                );

            // Test success because the key is activated.
            Assert.True(policy.DoesTransactionFollowsPolicy(txByNewActivated, blockChain));

            var singleAction = new PolymorphicAction<ActionBase>[]
            {
                new DailyReward(),
            };
            var manyActions = new PolymorphicAction<ActionBase>[]
            {
                new DailyReward(),
                new DailyReward(),
            };
            Transaction<PolymorphicAction<ActionBase>> txWithSingleAction =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    singleAction
                );
            Transaction<PolymorphicAction<ActionBase>> txWithManyActions =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    manyActions
                );

            // Transaction with more than two actions is rejected.
            Assert.True(policy.DoesTransactionFollowsPolicy(txWithSingleAction, blockChain));
            Assert.False(policy.DoesTransactionFollowsPolicy(txWithManyActions, blockChain));
        }

        [Fact]
        public void DoesTransactionFollowsPolicyWithAuthorizedMiners()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var authorizedMinerPrivateKey = new PrivateKey();

            (ActivationKey ak, PendingActivationState ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01 }
            );

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(10000, 100);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.ToAddress() },
                    5,
                    10
                ),
                pendingActivations: new[] { ps }
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            Transaction<PolymorphicAction<ActionBase>> txFromAuthorizedMiner =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    authorizedMinerPrivateKey,
                    genesis.Hash,
                    new PolymorphicAction<ActionBase>[] { ak.CreateActivateAccount(new byte[] { 0x00, 0x01 }) }
                );

            // Deny tx even if contains valid activation key.
            Assert.False(policy.DoesTransactionFollowsPolicy(txFromAuthorizedMiner, blockChain));
        }

        [Fact]
        public void MustNotIncludeBlockActionAtTransaction()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var authorizedMinerPrivateKey = new PrivateKey();

            (ActivationKey ak, PendingActivationState ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01 }
            );

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(10000, 100);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.ToAddress() },
                    5,
                    10
                ),
                pendingActivations: new[] { ps }
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            Assert.Throws<MissingActionTypeException>(() =>
            {
                blockChain.MakeTransaction(
                    adminPrivateKey,
                    new PolymorphicAction<ActionBase>[] { new RewardGold() }
                );
            });
        }

        [Fact]
        public async void EarnMiningGoldWhenSuccessMining()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var authorizedMinerPrivateKey = new PrivateKey();

            (ActivationKey ak, PendingActivationState ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01 }
            );

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(10000, 100);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.ToAddress() },
                    5,
                    10
                ),
                pendingActivations: new[] { ps }
            );

            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );

            await blockChain.MineBlock(adminAddress);
            FungibleAssetValue actualBalance = blockChain.GetBalance(adminAddress, _currency);
            FungibleAssetValue expectedBalance = new FungibleAssetValue(_currency, 10, 0);
            Assert.True(expectedBalance.Equals(actualBalance));
        }

        [Fact]
        public async Task ValidateNextBlockWithAuthorizedMinersState()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var minerKeys = new[] { new PrivateKey(), new PrivateKey() };
            Address[] miners = minerKeys.Select(AddressExtensions.ToAddress).ToArray();
            var stranger = new Address(
                new byte[]
                {
                    0x03, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00,
                }
            );

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                10000,
                100,
                ignoreHardcodedIndicesForBackwardCompatibility: true
            );
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet<Address>.Empty,
                new AuthorizedMinersState(miners, 2, 4)
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            if (policy is BlockPolicy bp)
            {
                bp.AuthorizedMinersState = new AuthorizedMinersState(
                    (Dictionary)blockChain.GetState(AuthorizedMinersState.Address)
                    );
            }

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            await blockChain.MineBlock(stranger);

            await Assert.ThrowsAsync<InvalidMinerException>(async () =>
            {
                await blockChain.MineBlock(stranger);
            });

            new Miner(blockChain, null, minerKeys[0], true).StageProofTransaction();
            await blockChain.MineBlock(miners[0]);

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            // it's okay because next block index is 3
            await blockChain.MineBlock(stranger);

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            // it isn't :(
            await Assert.ThrowsAsync<InvalidMinerException>(async () =>
            {
                await blockChain.MineBlock(stranger);
            });

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            // the authorization block should be proved by a proof tx
            await Assert.ThrowsAsync<InvalidMinerException>(
                async () => await blockChain.MineBlock(miners[1])
            );

            // the proof tx should be signed by the same authorized miner
            var othersProof = Transaction<PolymorphicAction<ActionBase>>.Create(
                blockChain.GetNextTxNonce(miners[0]),
                minerKeys[0],
                blockChain.Genesis.Hash,
                new PolymorphicAction<ActionBase>[0]
            );
            blockChain.StageTransaction(othersProof);
            await Assert.ThrowsAsync<InvalidMinerException>(
                async () => await blockChain.MineBlock(miners[1])
            );

            // the proof tx should be no-op
            var action = new PolymorphicAction<ActionBase>(
                new TransferAsset(miners[1], miners[0], new Currency("FOO", 0, miners[1]) * 1)
            );
            var nonEmptyProof = Transaction<PolymorphicAction<ActionBase>>.Create(
                blockChain.GetNextTxNonce(miners[1]),
                minerKeys[1],
                blockChain.Genesis.Hash,
                new[] { action }
            );
            blockChain.StageTransaction(nonEmptyProof);
            await Assert.ThrowsAsync<InvalidMinerException>(
                async () => await blockChain.MineBlock(miners[1])
            );

            new Miner(blockChain, null, minerKeys[1], true).StageProofTransaction();
            await blockChain.MineBlock(miners[1]);

            // it's okay because block index exceeds limitations.
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            await blockChain.MineBlock(stranger);
        }

        [Fact]
        public async Task GetNextBlockDifficultyWithAuthorizedMinersState()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var minerKey = new PrivateKey();
            var miner = minerKey.ToAddress();
            var miners = new[] { miner };

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(4096, 100);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet<Address>.Empty,
                new AuthorizedMinersState(miners, 2, 6)
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null), new DefaultKeyValueStore(null));
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );
            var minerObj = new Miner(blockChain, null, minerKey, true);

            if (policy is BlockPolicy bp)
            {
                bp.AuthorizedMinersState = new AuthorizedMinersState(
                    (Dictionary)blockChain.GetState(AuthorizedMinersState.Address)
                    );
            }

            var dateTimeOffset = DateTimeOffset.MinValue;

            // Index 1
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 2, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            minerObj.StageProofTransaction();
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 3
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 4, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            minerObj.StageProofTransaction();
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 5
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 6, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            minerObj.StageProofTransaction();
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 7
            Assert.Equal(4098, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 8
            Assert.Equal(4100, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 9
            Assert.Equal(4102, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 10
            Assert.Equal(4104, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 11
            Assert.Equal(4106, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(20);
            await blockChain.MineBlock(miner, dateTimeOffset);

            // Index 12
            Assert.Equal(4104, policy.GetNextBlockDifficulty(blockChain));
        }

        [Fact]
        public void ValidateNextBlockWithManyTransactions()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = new Address(adminPrivateKey.PublicKey);
            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(3000, 10);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(adminAddress, ImmutableHashSet<Address>.Empty);

            using var store = new DefaultStore(null);
            var stateStore = new NoOpStateStore();
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis
            );

            int nonce = 0;
            List<Transaction<PolymorphicAction<ActionBase>>> GenerateTransactions(int count)
            {
                var list = new List<Transaction<PolymorphicAction<ActionBase>>>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(Transaction<PolymorphicAction<ActionBase>>.Create(
                        nonce++,
                        adminPrivateKey,
                        genesis.Hash,
                        new PolymorphicAction<ActionBase>[] { }
                    ));
                }

                return list;
            }

            Assert.Equal(1, blockChain.Count);
            Block<PolymorphicAction<ActionBase>> block1 = Block<PolymorphicAction<ActionBase>>.Mine(
                index: 1,
                hashAlgorithm: policy.GetHashAlgorithm(1),
                difficulty: policy.GetNextBlockDifficulty(blockChain),
                previousTotalDifficulty: blockChain.Tip.TotalDifficulty,
                miner: adminAddress,
                previousHash: blockChain.Tip.Hash,
                timestamp: DateTimeOffset.MinValue,
                transactions: GenerateTransactions(5));
            blockChain.Append(block1);
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));
            Block<PolymorphicAction<ActionBase>> block2 = Block<PolymorphicAction<ActionBase>>.Mine(
                index: 2,
                hashAlgorithm: policy.GetHashAlgorithm(2),
                difficulty: policy.GetNextBlockDifficulty(blockChain),
                previousTotalDifficulty: blockChain.Tip.TotalDifficulty,
                miner: adminAddress,
                previousHash: blockChain.Tip.Hash,
                timestamp: DateTimeOffset.MinValue,
                transactions: GenerateTransactions(10));
            blockChain.Append(block2);
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block2.Hash));
            Block<PolymorphicAction<ActionBase>> block3 = Block<PolymorphicAction<ActionBase>>.Mine(
                index: 3,
                hashAlgorithm: policy.GetHashAlgorithm(3),
                difficulty: policy.GetNextBlockDifficulty(blockChain),
                previousTotalDifficulty: blockChain.Tip.TotalDifficulty,
                miner: adminAddress,
                previousHash: blockChain.Tip.Hash,
                timestamp: DateTimeOffset.MinValue,
                transactions: GenerateTransactions(11));
            Assert.Throws<BlockExceedingTransactionsException>(() => blockChain.Append(block3));
            Assert.Equal(3, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block3.Hash));
        }

        private Block<PolymorphicAction<ActionBase>> MakeGenesisBlock(
            Address adminAddress,
            IImmutableSet<Address> activatedAddresses,
            AuthorizedMinersState authorizedMinersState = null,
            DateTimeOffset? timestamp = null,
            PendingActivationState[] pendingActivations = null
        )
        {
            if (pendingActivations is null)
            {
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
                (ActivationKey activationKey, PendingActivationState pendingActivation) =
                    ActivationKey.Create(_privateKey, nonce);
                pendingActivations = new[] { pendingActivation };
            }

            var sheets = TableSheetsImporter.ImportSheets();
            return BlockHelper.MineGenesisBlock(
                sheets,
                new GoldDistribution[0],
                pendingActivations,
                new AdminState(adminAddress, 1500000),
                authorizedMinersState: authorizedMinersState,
                activatedAccounts: activatedAddresses,
                isActivateAdminAddress: false,
                credits: null,
                privateKey: _privateKey,
                timestamp: timestamp ?? DateTimeOffset.MinValue);
        }

        private class NoOpStateStore : IStateStore
        {
            public void SetStates<T>(Block<T> block, IImmutableDictionary<string, IValue> states)
                where T : IAction, new()
            {
            }

            public IValue GetState(string stateKey, BlockHash? blockHash = null) =>
                default(Null);

            public bool ContainsBlockStates(BlockHash blockHash) => false;

            public void ForkStates<T>(Guid sourceChainId, Guid destinationChainId, Block<T> branchpoint)
                where T : IAction, new()
            {
            }
        }
    }
}
