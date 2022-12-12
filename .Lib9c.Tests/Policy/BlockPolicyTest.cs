namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
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
    using Nekoyume.BlockChain.Policy;
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
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, _privateKey.ToAddress());
#pragma warning restore CS0618
        }

        [Fact]
        public async Task ValidateNextBlockTx()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                10_000, null, null, null, null, null, null, null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress)
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
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
            Assert.NotNull(policy.ValidateNextBlockTx(blockChain, txByStranger));

            var newActivatedPrivateKey = new PrivateKey();
            var newActivatedAddress = newActivatedPrivateKey.ToAddress();

            // Activate with admin account.
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new AddActivatedAccount(newActivatedAddress) }
            );
            await blockChain.MineBlock(adminPrivateKey);

            Transaction<PolymorphicAction<ActionBase>> txByNewActivated =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    new PolymorphicAction<ActionBase>[] { }
                );

            // Test success because the key is activated.
            Assert.Null(policy.ValidateNextBlockTx(blockChain, txByNewActivated));

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
            Assert.Null(policy.ValidateNextBlockTx(blockChain, txWithSingleAction));
            Assert.NotNull(policy.ValidateNextBlockTx(blockChain, txWithManyActions));
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
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                10_000, null, null, null, null, null, null, null);
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
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
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
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                10_000, null, null, null, null, null, null, null);
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
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
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

            await blockChain.MineBlock(adminPrivateKey);
            FungibleAssetValue actualBalance = blockChain.GetBalance(adminAddress, _currency);
            FungibleAssetValue expectedBalance = new FungibleAssetValue(_currency, 10, 0);
            Assert.True(expectedBalance.Equals(actualBalance));
        }

        [Fact]
        public async Task ValidateNextBlockWithAuthorizedMinersPolicy()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var minerKeys = new[] { new PrivateKey(), new PrivateKey() };
            Address[] miners = minerKeys.Select(AddressExtensions.ToAddress).ToArray();
            var stranger = new PrivateKey();

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                minimumDifficulty: 10_000,
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: null,
                maxTransactionsPerSignerPerBlockPolicy: null,
                authorizedMinersPolicy: AuthorizedMinersPolicy
                    .Default
                    .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                        startIndex: 0,
                        endIndex: 4,
                        filter: index => index % 2 == 0,
                        value: miners.ToImmutableHashSet())),
                permissionedMinersPolicy: null,
                minBlockProtocolVersionPolicy: null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet<Address>.Empty);
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
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

            // Index 1. Anyone can mine.
            await blockChain.MineBlock(stranger);

            // Index 2. Only authorized miner can mine.
            await Assert.ThrowsAsync<BlockPolicyViolationException>(async () =>
            {
                await blockChain.MineBlock(stranger);
            });
            // Old proof mining still works.
            await blockChain.MineBlock(minerKeys[0]);

            // Index 3. Anyone can mine.
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            await blockChain.MineBlock(stranger);

            // Index 4. Again, only authorized miner can mine.
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            await Assert.ThrowsAsync<BlockPolicyViolationException>(async () =>
            {
                await blockChain.MineBlock(stranger);
            });
            // No proof is required.
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            await blockChain.MineBlock(minerKeys[1]);

            // Index 5, 6. Anyone can mine.
            await blockChain.MineBlock(stranger);
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            await blockChain.MineBlock(stranger);
        }

        [Fact]
        public async Task GetNextBlockDifficultyWithAuthorizedMinersPolicy()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var minerKey = new PrivateKey();
            var miners = new[] { minerKey.ToAddress() };

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                minimumDifficulty: 4096,
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: null,
                maxTransactionsPerSignerPerBlockPolicy: null,
                authorizedMinersPolicy: AuthorizedMinersPolicy
                    .Default
                    .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                        startIndex: 0,
                        endIndex: 6,
                        filter: index => index % 2 == 0,
                        value: miners.ToImmutableHashSet())),
                permissionedMinersPolicy: null,
                minBlockProtocolVersionPolicy: null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet<Address>.Empty);
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            var dateTimeOffset = DateTimeOffset.MinValue;

            // Index 1
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 2, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 3
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 4, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 5
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 6, target index
            Assert.Equal(4096, policy.GetNextBlockDifficulty(blockChain));

            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 7
            Assert.Equal(4098, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 8
            Assert.Equal(4100, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 9
            Assert.Equal(4102, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 10
            Assert.Equal(4104, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(1);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 11
            Assert.Equal(4106, policy.GetNextBlockDifficulty(blockChain));
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            dateTimeOffset += TimeSpan.FromSeconds(20);
            await blockChain.MineBlock(minerKey, dateTimeOffset);

            // Index 12
            Assert.Equal(4104, policy.GetNextBlockDifficulty(blockChain));
        }

        [Fact]
        public void ValidateNextBlockWithManyTransactions()
        {
            var adminPrivateKey = new PrivateKey();
            var adminPublicKey = adminPrivateKey.PublicKey;
            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                minimumDifficulty: 10_000,
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(0, null, null, 10)),
                maxTransactionsPerSignerPerBlockPolicy: null,
                authorizedMinersPolicy: null,
                permissionedMinersPolicy: null,
                minBlockProtocolVersionPolicy: null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis =
                MakeGenesisBlock(adminPublicKey.ToAddress(), ImmutableHashSet<Address>.Empty);

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
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
            var txs1 = GenerateTransactions(5).OrderBy(tx => tx.Id).ToArray();
            Block<PolymorphicAction<ActionBase>> block1 = new BlockContent<PolymorphicAction<ActionBase>>(
                new BlockMetadata(
                    index: 1L,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    difficulty: policy.GetNextBlockDifficulty(blockChain),
                    totalDifficulty: blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent<PolymorphicAction<ActionBase>>.DeriveTxHash(txs1)),
                txs1).Mine().Evaluate(adminPrivateKey, blockChain);
            blockChain.Append(block1);
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));

            var txs2 = GenerateTransactions(10).OrderBy(tx => tx.Id).ToArray();
            Block<PolymorphicAction<ActionBase>> block2 = new BlockContent<PolymorphicAction<ActionBase>>(
                new BlockMetadata(
                    index: 2L,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    difficulty: policy.GetNextBlockDifficulty(blockChain),
                    totalDifficulty: blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent<PolymorphicAction<ActionBase>>.DeriveTxHash(txs2)),
                txs2).Mine().Evaluate(adminPrivateKey, blockChain);
            blockChain.Append(block2);
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block2.Hash));

            var txs3 = GenerateTransactions(11).OrderBy(tx => tx.Id).ToArray();
            Block<PolymorphicAction<ActionBase>> block3 = new BlockContent<PolymorphicAction<ActionBase>>(
                new BlockMetadata(
                    index: 3L,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    difficulty: policy.GetNextBlockDifficulty(blockChain),
                    totalDifficulty: blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent<PolymorphicAction<ActionBase>>.DeriveTxHash(txs3)),
                txs3).Mine().Evaluate(adminPrivateKey, blockChain);
            Assert.Throws<InvalidBlockTxCountException>(() => blockChain.Append(block3));
            Assert.Equal(3, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block3.Hash));
        }

        [Fact]
        public void ValidateNextBlockWithManyTransactionsPerSigner()
        {
            var adminPrivateKey = new PrivateKey();
            var adminPublicKey = adminPrivateKey.PublicKey;
            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                minimumDifficulty: 10_000,
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(0, null, null, 10)),
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(2, null, null, 5)),
                authorizedMinersPolicy: null,
                permissionedMinersPolicy: null,
                minBlockProtocolVersionPolicy: null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis =
                MakeGenesisBlock(adminPublicKey.ToAddress(), ImmutableHashSet<Address>.Empty);

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
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

            var txs1 = GenerateTransactions(10).OrderBy(tx => tx.Id).ToArray();
            Block<PolymorphicAction<ActionBase>> block1 = new BlockContent<PolymorphicAction<ActionBase>>(
                new BlockMetadata(
                    index: 1L,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    difficulty: policy.GetNextBlockDifficulty(blockChain),
                    totalDifficulty: blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent<PolymorphicAction<ActionBase>>.DeriveTxHash(txs1)),
                txs1).Mine().Evaluate(adminPrivateKey, blockChain);

            // Should be fine since policy hasn't kicked in yet.
            blockChain.Append(block1);
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));

            var txs2 = GenerateTransactions(10).OrderBy(tx => tx.Id).ToArray();
            Block<PolymorphicAction<ActionBase>> block2 = new BlockContent<PolymorphicAction<ActionBase>>(
                new BlockMetadata(
                    index: 2L,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    difficulty: policy.GetNextBlockDifficulty(blockChain),
                    totalDifficulty: blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent<PolymorphicAction<ActionBase>>.DeriveTxHash(txs2)),
                txs2).Mine().Evaluate(adminPrivateKey, blockChain);

            // Subpolicy kicks in.
            Assert.Throws<InvalidBlockTxCountPerSignerException>(() => blockChain.Append(block2));
            Assert.Equal(2, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block2.Hash));
            // Since failed, roll back nonce.
            nonce -= 10;

            // Limit should also pass.
            var txs3 = GenerateTransactions(5).OrderBy(tx => tx.Id).ToArray();
            Block<PolymorphicAction<ActionBase>> block3 = new BlockContent<PolymorphicAction<ActionBase>>(
                new BlockMetadata(
                    index: 2L,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    difficulty: policy.GetNextBlockDifficulty(blockChain),
                    totalDifficulty: blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent<PolymorphicAction<ActionBase>>.DeriveTxHash(txs3)),
                txs3).Mine().Evaluate(adminPrivateKey, blockChain);

            blockChain.Append(block3);
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block3.Hash));
        }

        [Fact]
        public void ValidateNextBlockWithLowBlockProtocolVersion()
        {
            var adminPrivateKey = new PrivateKey();
            var adminPublicKey = adminPrivateKey.PublicKey;
            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                minimumDifficulty: 10_000,
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: null,
                maxTransactionsPerSignerPerBlockPolicy: null,
                authorizedMinersPolicy: null,
                permissionedMinersPolicy: null,
                minBlockProtocolVersionPolicy: MinBlockProtocolVersionPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(2, null, null, 4)));
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis =
                MakeGenesisBlock(adminPublicKey.ToAddress(), ImmutableHashSet<Address>.Empty);

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis
            );

            Assert.Equal(1, blockChain.Count);
            Block<PolymorphicAction<ActionBase>> block1 = new BlockContent<PolymorphicAction<ActionBase>>(
                new BlockMetadata(
                    index: 1L,
                    timestamp: DateTimeOffset.UtcNow,
                    publicKey: adminPublicKey,
                    difficulty: policy.GetNextBlockDifficulty(blockChain),
                    totalDifficulty: blockChain.Tip.Difficulty + policy.GetNextBlockDifficulty(blockChain),
                    previousHash: blockChain.Tip.Hash,
                    txHash: null)).Mine().Evaluate(adminPrivateKey, blockChain);

            // Should be fine since policy hasn't kicked in yet.
            blockChain.Append(block1);
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));
            Assert.Equal(3, block1.ProtocolVersion);

            Block<PolymorphicAction<ActionBase>> block2 = new BlockContent<PolymorphicAction<ActionBase>>(
                new BlockMetadata(
                    index: 2L,
                    timestamp: DateTimeOffset.UtcNow,
                    publicKey: adminPublicKey,
                    difficulty: policy.GetNextBlockDifficulty(blockChain),
                    totalDifficulty: blockChain.Tip.Difficulty + policy.GetNextBlockDifficulty(blockChain),
                    previousHash: blockChain.Tip.Hash,
                    txHash: null)).Mine().Evaluate(adminPrivateKey, blockChain);

            // Subpolicy kicks in.
            Assert.Throws<BlockPolicyViolationException>(() => blockChain.Append(block2));
            Assert.Equal(2, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block2.Hash));
            Assert.Equal(3, block2.ProtocolVersion);
            Assert.Equal(4, blockChain.Policy.GetMinBlockProtocolVersion(block2.Index));
        }

        [Fact]
        public async Task PermissionedBlockPolicy()
        {
            // This creates genesis with _privateKey as its miner.
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var permissionedMinerKey = new PrivateKey();
            var nonPermissionedMinerKey = new PrivateKey();
            var pendingActivations = new[]
            {
                permissionedMinerKey,
                nonPermissionedMinerKey,
            }.Select(key => ActivationKey.Create(key, nonce).Item2).ToArray();

            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                default(Address),
                ImmutableHashSet<Address>.Empty,
                pendingActivations: pendingActivations);
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockPolicySource = new BlockPolicySource(Logger.None);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                blockPolicySource.GetPolicy(
                    minimumDifficulty: 10_000,
                    maxTransactionsBytesPolicy: null,
                    minTransactionsPerBlockPolicy: null,
                    maxTransactionsPerBlockPolicy: null,
                    maxTransactionsPerSignerPerBlockPolicy: null,
                    authorizedMinersPolicy: null,
                    permissionedMinersPolicy: PermissionedMinersPolicy
                        .Default
                        .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                            startIndex: 1,
                            endIndex: null,
                            filter: null,
                            value: new Address[] { permissionedMinerKey.ToAddress() }
                                .ToImmutableHashSet())),
                    minBlockProtocolVersionPolicy: null),
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>(),
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            // Old proof mining is still allowed.
            blockChain.StageTransaction(Transaction<PolymorphicAction<ActionBase>>.Create(
                0,
                permissionedMinerKey,
                genesis.Hash,
                new PolymorphicAction<ActionBase>[] { }
            ));
            await blockChain.MineBlock(permissionedMinerKey);

            // Bad proof can also be mined.
            blockChain.StageTransaction(Transaction<PolymorphicAction<ActionBase>>.Create(
                0,
                nonPermissionedMinerKey,
                genesis.Hash,
                new PolymorphicAction<ActionBase>[] { }
            ));
            await blockChain.MineBlock(permissionedMinerKey);

            await blockChain.MineBlock(permissionedMinerKey);

            // Error, it isn't permissioned miner.
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(nonPermissionedMinerKey));
        }

        [Fact]
        public async Task MixedMiningPolicy()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var authorizedMinerKey = new PrivateKey();
            var permissionedMinerKey = new PrivateKey();
            var someMinerKey = new PrivateKey();
            var addresses = new Address[]
            {
                authorizedMinerKey.ToAddress(),
                permissionedMinerKey.ToAddress(),
                someMinerKey.ToAddress(),
            };
            var pendingActivations = new[]
            {
                authorizedMinerKey,
                permissionedMinerKey,
                someMinerKey,
            }.Select(key => ActivationKey.Create(key, nonce).Item2).ToArray();
            var action = new TransferAsset(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                new FungibleAssetValue(_currency, 0, 0));

            // This creates genesis with _privateKey as its miner.
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                default(Address),
                ImmutableHashSet<Address>.Empty,
                pendingActivations: pendingActivations);
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockPolicySource = new BlockPolicySource(Logger.None);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                blockPolicySource.GetPolicy(
                    minimumDifficulty: 10_000,
                    maxTransactionsBytesPolicy: null,
                    minTransactionsPerBlockPolicy: null,
                    maxTransactionsPerBlockPolicy: null,
                    maxTransactionsPerSignerPerBlockPolicy: null,
                    authorizedMinersPolicy: AuthorizedMinersPolicy
                        .Default
                        .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                            startIndex: 0,
                            endIndex: 6,
                            filter: index => index % 2 == 0,
                            value: new Address[] { authorizedMinerKey.ToAddress() }
                                .ToImmutableHashSet())),
                    permissionedMinersPolicy: PermissionedMinersPolicy
                        .Default
                        .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                            startIndex: 2,
                            endIndex: 10,
                            filter: index => index % 3 == 0,
                            value: new Address[] { permissionedMinerKey.ToAddress() }
                                .ToImmutableHashSet())),
                    minBlockProtocolVersionPolicy: null),
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>(),
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            Transaction<PolymorphicAction<ActionBase>> proof;

            // Index 1: Anyone can mine.
            await blockChain.MineBlock(someMinerKey);

            // Index 2: Only authorized miner can mine.
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(permissionedMinerKey));
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(someMinerKey));
            await blockChain.MineBlock(authorizedMinerKey);

            // Index 3: Only permissioned miner can mine.
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(authorizedMinerKey));
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(someMinerKey));
            await blockChain.MineBlock(permissionedMinerKey);

            // Index 4: Only authorized miner can mine.
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(permissionedMinerKey));
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(someMinerKey));
            await blockChain.MineBlock(authorizedMinerKey);

            // Index 5: Anyone can mine again.
            await blockChain.MineBlock(someMinerKey);

            // Index 6: In case both authorized mining and permissioned mining apply,
            // only authorized miner can mine.
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(permissionedMinerKey));
            await blockChain.MineBlock(authorizedMinerKey);

            // Index 7, 8, 9: Check authorized mining ended.
            await blockChain.MineBlock(someMinerKey);
            await blockChain.MineBlock(someMinerKey);
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(someMinerKey));
            proof = blockChain.MakeTransaction(
                permissionedMinerKey,
                new PolymorphicAction<ActionBase>[] { action });
            await blockChain.MineBlock(permissionedMinerKey);

            // Index 10, 11, 12: Check permissioned mining ended.
            await blockChain.MineBlock(someMinerKey);
            await blockChain.MineBlock(someMinerKey);
            await blockChain.MineBlock(someMinerKey);

            // Index 13, 14: Check authorized miner and permissioned miner can also mine
            // when policy is allowed for all miners.
            await blockChain.MineBlock(authorizedMinerKey);
            await blockChain.MineBlock(permissionedMinerKey);
        }

        [Fact]
        public void IsAllowedtoMine()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var authorizedMinerKey = new PrivateKey();
            var permissionedMinerKey = new PrivateKey();
            var someMinerKey = new PrivateKey();
            var addresses = new Address[]
            {
                authorizedMinerKey.ToAddress(),
                permissionedMinerKey.ToAddress(),
                someMinerKey.ToAddress(),
            };
            var pendingActivations = new[]
            {
                authorizedMinerKey,
                permissionedMinerKey,
                someMinerKey,
            }.Select(key => ActivationKey.Create(key, nonce).Item2).ToArray();

            // This creates genesis with _privateKey as its miner.
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                default(Address),
                ImmutableHashSet<Address>.Empty,
                pendingActivations: pendingActivations);
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockPolicySource = new BlockPolicySource(Logger.None);

            var policy = (BlockPolicy)blockPolicySource.GetPolicy(
                    minimumDifficulty: 10_000,
                    maxTransactionsBytesPolicy: null,
                    minTransactionsPerBlockPolicy: null,
                    maxTransactionsPerBlockPolicy: null,
                    maxTransactionsPerSignerPerBlockPolicy: null,
                    authorizedMinersPolicy: AuthorizedMinersPolicy
                        .Default
                        .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                            startIndex: 0,
                            endIndex: 10,
                            filter: index => index % 2 == 0,
                            value: new Address[] { authorizedMinerKey.ToAddress() }
                                .ToImmutableHashSet())),
                    permissionedMinersPolicy: PermissionedMinersPolicy
                        .Default
                        .Add(new SpannedSubPolicy<ImmutableHashSet<Address>>(
                            startIndex: 5,
                            endIndex: 20,
                            filter: index => index % 3 == 0,
                            value: new Address[] { permissionedMinerKey.ToAddress() }
                                .ToImmutableHashSet())),
                    minBlockProtocolVersionPolicy: null);

            // For genesis, any miner is allowed.
            Assert.All(addresses, address => Assert.True(policy.IsAllowedToMine(address, 0)));
            // Same goes for the next one.
            Assert.All(addresses, address => Assert.True(policy.IsAllowedToMine(address, 1)));
            // Only authorized miner should be allowed for index 2.
            Assert.True(policy.IsAllowedToMine(authorizedMinerKey.ToAddress(), 2));
            Assert.False(policy.IsAllowedToMine(permissionedMinerKey.ToAddress(), 2));
            Assert.False(policy.IsAllowedToMine(someMinerKey.ToAddress(), 2));
            // Only authorized miner should be allowed for index 6.
            Assert.True(policy.IsAllowedToMine(authorizedMinerKey.ToAddress(), 6));
            Assert.False(policy.IsAllowedToMine(permissionedMinerKey.ToAddress(), 6));
            Assert.False(policy.IsAllowedToMine(someMinerKey.ToAddress(), 6));
            // Any miner should be able to mine for index 7.
            Assert.True(policy.IsAllowedToMine(authorizedMinerKey.ToAddress(), 7));
            Assert.True(policy.IsAllowedToMine(permissionedMinerKey.ToAddress(), 7));
            Assert.True(policy.IsAllowedToMine(someMinerKey.ToAddress(), 7));
            // Only permissioned miner should be allowed for index 9.
            Assert.False(policy.IsAllowedToMine(authorizedMinerKey.ToAddress(), 9));
            Assert.True(policy.IsAllowedToMine(permissionedMinerKey.ToAddress(), 9));
            Assert.False(policy.IsAllowedToMine(someMinerKey.ToAddress(), 9));
            // Only permissioned miner should be allowed for index 12.
            Assert.False(policy.IsAllowedToMine(authorizedMinerKey.ToAddress(), 12));
            Assert.True(policy.IsAllowedToMine(permissionedMinerKey.ToAddress(), 12));
            Assert.False(policy.IsAllowedToMine(someMinerKey.ToAddress(), 12));
            // Any miner should be able to mine 24.
            Assert.True(policy.IsAllowedToMine(authorizedMinerKey.ToAddress(), 24));
            Assert.True(policy.IsAllowedToMine(permissionedMinerKey.ToAddress(), 24));
            Assert.True(policy.IsAllowedToMine(someMinerKey.ToAddress(), 24));
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
    }
}
