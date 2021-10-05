namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text.RegularExpressions;
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
            _currency = new Currency("NCG", 2, minter: _privateKey.ToAddress());
        }

        [Fact]
        public async Task ValidateNextBlockTx()
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
            await blockChain.MineBlock(adminAddress);

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
        public void ValidateNextBlockTxWithAuthorizedMiners()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var authorizedMinerPrivateKey = new PrivateKey();

            (ActivationKey ak, PendingActivationState ps) = ActivationKey.Create(
                new PrivateKey(),
                new byte[] { 0x00, 0x01 }
            );

            var blockPolicySource = new BlockPolicySource(Logger.None);
            AuthorizedMiningPolicy authorizedMiningPolicy = new AuthorizedMiningPolicy(
                startIndex: 0,
                endIndex: 10,
                interval: 5,
                miners: new[] { authorizedMinerPrivateKey.ToAddress() }.ToHashSet());
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                minimumDifficulty: 10000,
                maxTransactionsPerBlock: 100,
                ignoreHardcodedPolicies: false,
                authorizedMiningPolicy: authorizedMiningPolicy,
                permissionedMiningPolicy: null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
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

            Transaction<PolymorphicAction<ActionBase>> txFromAuthorizedMiner =
                Transaction<PolymorphicAction<ActionBase>>.Create(
                    0,
                    authorizedMinerPrivateKey,
                    genesis.Hash,
                    new PolymorphicAction<ActionBase>[] { ak.CreateActivateAccount(new byte[] { 0x00, 0x01 }) }
                );

            // Deny tx even if contains valid activation key.
            Assert.NotNull(policy.ValidateNextBlockTx(blockChain, txFromAuthorizedMiner));
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
            AuthorizedMiningPolicy authorizedMiningPolicy = new AuthorizedMiningPolicy(
                miners: miners.ToHashSet(),
                startIndex: 0,
                endIndex: 4,
                interval: 2);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                10000,
                100,
                ignoreHardcodedPolicies: true,
                authorizedMiningPolicy: authorizedMiningPolicy,
                permissionedMiningPolicy: null);
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

            await blockChain.MineBlock(stranger);

            await Assert.ThrowsAsync<BlockPolicyViolationException>(async () =>
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
            await Assert.ThrowsAsync<BlockPolicyViolationException>(async () =>
            {
                await blockChain.MineBlock(stranger);
            });

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );
            // the authorization block should be proved by a proof tx
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
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
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
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
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
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
            AuthorizedMiningPolicy authorizedMiningPolicy = new AuthorizedMiningPolicy(
                miners: miners.ToHashSet(),
                startIndex: 0,
                endIndex: 6,
                interval: 2);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                minimumDifficulty: 4096,
                maxTransactionsPerBlock: 100,
                ignoreHardcodedPolicies: false,
                authorizedMiningPolicy: authorizedMiningPolicy,
                permissionedMiningPolicy: null);
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
            var minerObj = new Miner(blockChain, null, minerKey, true);

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
            Block<PolymorphicAction<ActionBase>> block1 = new BlockContent<PolymorphicAction<ActionBase>>
            {
                Index = 1,
                Difficulty = policy.GetNextBlockDifficulty(blockChain),
                TotalDifficulty =
                    blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                Miner = adminAddress,
                PreviousHash = blockChain.Tip.Hash,
                Timestamp = DateTimeOffset.MinValue,
                Transactions = GenerateTransactions(5),
            }.Mine(policy.GetHashAlgorithm(1)).Evaluate(blockChain);
            blockChain.Append(block1);
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));
            Block<PolymorphicAction<ActionBase>> block2 = new BlockContent<PolymorphicAction<ActionBase>>
            {
                Index = 2,
                Difficulty = policy.GetNextBlockDifficulty(blockChain),
                TotalDifficulty =
                    blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                Miner = adminAddress,
                PreviousHash = blockChain.Tip.Hash,
                Timestamp = DateTimeOffset.MinValue,
                Transactions = GenerateTransactions(10),
            }.Mine(policy.GetHashAlgorithm(2)).Evaluate(blockChain);
            blockChain.Append(block2);
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block2.Hash));
            Block<PolymorphicAction<ActionBase>> block3 = new BlockContent<PolymorphicAction<ActionBase>>
            {
                Index = 3,
                Difficulty = policy.GetNextBlockDifficulty(blockChain),
                TotalDifficulty =
                    blockChain.Tip.TotalDifficulty + policy.GetNextBlockDifficulty(blockChain),
                Miner = adminAddress,
                PreviousHash = blockChain.Tip.Hash,
                Timestamp = DateTimeOffset.MinValue,
                Transactions = GenerateTransactions(11),
            }.Mine(policy.GetHashAlgorithm(3)).Evaluate(blockChain);
            Assert.Throws<BlockExceedingTransactionsException>(() => blockChain.Append(block3));
            Assert.Equal(3, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block3.Hash));
        }

        [Fact]
        public void Obsolete_Actions()
        {
            Assert.Empty(Assembly.GetAssembly(typeof(ActionBase))!.GetTypes().Where(
                type => type.Namespace is { } @namespace &&
                        @namespace.StartsWith($"{nameof(Nekoyume)}.{nameof(Nekoyume.Action)}") &&
                        typeof(ActionBase).IsAssignableFrom(type) &&
                        !type.IsAbstract &&
                        Regex.IsMatch(type.Name, @"\d+$") &&
                        !type.IsDefined(typeof(ActionObsoleteAttribute), false)));
        }

        [Fact]
        public async Task PermissionedBlockPolicy()
        {
            // This creates genesis with _privateKey as its miner.
            Block<PolymorphicAction<ActionBase>> genesis = MakeGenesisBlock(
                default(Address),
                ImmutableHashSet<Address>.Empty);
            var permissionedMinerKey = _privateKey;
            var nonPermissionedMinerKey = new PrivateKey();
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockPolicySource = new BlockPolicySource(Logger.None);
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                blockPolicySource.GetPolicy(
                    minimumDifficulty: 50_000,
                    maxTransactionsPerBlock: 100,
                    authorizedMiningPolicy: null,
                    permissionedMiningPolicy: new PermissionedMiningPolicy(
                        miners: new[]
                        {
                            permissionedMinerKey.ToAddress(),
                        }.ToImmutableHashSet(),
                        startIndex: 1,
                        endIndex: null
                    ),
                    ignoreHardcodedPolicies: true
                ),
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>(),
                store,
                stateStore,
                genesis,
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            // Since activation transaction is attached to _privateKey,
            // Next nonce is 1.
            blockChain.StageTransaction(Transaction<PolymorphicAction<ActionBase>>.Create(
                1,
                permissionedMinerKey,
                genesis.Hash,
                new PolymorphicAction<ActionBase>[] { }
            ));
            await blockChain.MineBlock(permissionedMinerKey.ToAddress());

            // Error, there is no proof tx.
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(permissionedMinerKey.ToAddress()));

            // Error, it's invalid proof
            blockChain.StageTransaction(Transaction<PolymorphicAction<ActionBase>>.Create(
                0,
                nonPermissionedMinerKey,
                genesis.Hash,
                new PolymorphicAction<ActionBase>[] { }
            ));
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(permissionedMinerKey.ToAddress()));

            // Error, it isn't permissioned miner.
            await Assert.ThrowsAsync<BlockPolicyViolationException>(
                () => blockChain.MineBlock(nonPermissionedMinerKey.ToAddress()));
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
