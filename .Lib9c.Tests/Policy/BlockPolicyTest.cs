namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.Loader;
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.Consensus;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Libplanet.Tx;
    using Nekoyume;
    using Nekoyume.Action;
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
        public void ValidateNextBlockTx()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                null, null, null, null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                initialValidators: new Dictionary<PublicKey, BigInteger>
                { { adminPrivateKey.PublicKey, BigInteger.One } }
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = BlockChain<PolymorphicAction<ActionBase>>.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    policyBlockActionGetter: _ => policy.BlockAction,
                    blockChainStates: new BlockChainStates(store, stateStore),
                    actionTypeLoader: new SingleActionLoader(typeof(PolymorphicAction<ActionBase>)),
                    feeCalculator: null
                ),
                renderers: new[] { blockPolicySource.BlockRenderer }
            );
            Transaction txByStranger =
                Transaction.Create(
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
            Block block = blockChain.ProposeBlock(adminPrivateKey);
            blockChain.Append(block, GenerateBlockCommit(block, adminPrivateKey));

            Transaction txByNewActivated =
                Transaction.Create(
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
            Transaction txWithSingleAction =
                Transaction.Create(
                    0,
                    newActivatedPrivateKey,
                    genesis.Hash,
                    singleAction
                );
            Transaction txWithManyActions =
                Transaction.Create(
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
        public void BlockCommitFromNonValidator()
        {
            var adminPrivateKey = new PrivateKey();
            var adminAddress = adminPrivateKey.ToAddress();
            var nonValidator = new PrivateKey();

            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                null, null, null, null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                initialValidators: new Dictionary<PublicKey, BigInteger>
                { { adminPrivateKey.PublicKey, BigInteger.One } }
            );
            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = BlockChain<PolymorphicAction<ActionBase>>.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    policyBlockActionGetter: _ => policy.BlockAction,
                    blockChainStates: new BlockChainStates(store, stateStore),
                    actionTypeLoader: new SingleActionLoader(typeof(PolymorphicAction<ActionBase>)),
                    feeCalculator: null
                ),
                renderers: new[] { blockPolicySource.BlockRenderer }
            );
            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new AddActivatedAccount(adminPrivateKey.ToAddress()) }
            );
            Block block1 = blockChain.ProposeBlock(adminPrivateKey);
            Assert.Throws<InvalidBlockCommitException>(
                () => blockChain.Append(block1, GenerateBlockCommit(block1, nonValidator)));
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
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: null,
                maxTransactionsPerSignerPerBlockPolicy: null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block genesis = MakeGenesisBlock(
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
            var blockChain = BlockChain<PolymorphicAction<ActionBase>>.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    policyBlockActionGetter: _ => policy.BlockAction,
                    blockChainStates: new BlockChainStates(store, stateStore),
                    actionTypeLoader: new SingleActionLoader(typeof(PolymorphicAction<ActionBase>)),
                    feeCalculator: null
                ),
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
        public void EarnMiningGoldWhenSuccessMining()
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
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: null,
                maxTransactionsPerSignerPerBlockPolicy: null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block genesis = MakeGenesisBlock(
                adminAddress,
                ImmutableHashSet.Create(adminAddress),
                new AuthorizedMinersState(
                    new[] { authorizedMinerPrivateKey.ToAddress() },
                    5,
                    10
                ),
                new Dictionary<PublicKey, BigInteger> { { adminPrivateKey.PublicKey, BigInteger.One } },
                pendingActivations: new[] { ps }
            );

            using var store = new DefaultStore(null);
            using var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = BlockChain<PolymorphicAction<ActionBase>>.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    policyBlockActionGetter: _ => policy.BlockAction,
                    blockChainStates: new BlockChainStates(store, stateStore),
                    actionTypeLoader: new SingleActionLoader(typeof(PolymorphicAction<ActionBase>)),
                    feeCalculator: null
                ),
                renderers: new[] { blockPolicySource.BlockRenderer }
            );

            blockChain.MakeTransaction(
                adminPrivateKey,
                new PolymorphicAction<ActionBase>[] { new DailyReward(), }
            );

            Block block = blockChain.ProposeBlock(adminPrivateKey);
            blockChain.Append(block, GenerateBlockCommit(block, adminPrivateKey));
            FungibleAssetValue actualBalance = blockChain.GetBalance(adminAddress, _currency);
            FungibleAssetValue expectedBalance = new FungibleAssetValue(_currency, 10, 0);
            Assert.True(expectedBalance.Equals(actualBalance));
        }

        [Fact]
        public void ValidateNextBlockWithManyTransactions()
        {
            var adminPrivateKey = new PrivateKey();
            var adminPublicKey = adminPrivateKey.PublicKey;
            var blockPolicySource = new BlockPolicySource(Logger.None);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy(
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(0, null, null, 10)),
                maxTransactionsPerSignerPerBlockPolicy: null);
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block genesis =
                MakeGenesisBlock(
                    adminPublicKey.ToAddress(),
                    ImmutableHashSet<Address>.Empty,
                    initialValidators: new Dictionary<PublicKey, BigInteger>
                    { { adminPrivateKey.PublicKey, BigInteger.One } });

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var blockChain = BlockChain<PolymorphicAction<ActionBase>>.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    policyBlockActionGetter: _ => policy.BlockAction,
                    blockChainStates: new BlockChainStates(store, stateStore),
                    actionTypeLoader: new SingleActionLoader(typeof(PolymorphicAction<ActionBase>)),
                    feeCalculator: null
                )
            );

            int nonce = 0;
            List<Transaction> GenerateTransactions(int count)
            {
                var list = new List<Transaction>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(Transaction.Create(
                        nonce++,
                        adminPrivateKey,
                        genesis.Hash,
                        new PolymorphicAction<ActionBase>[] { }
                    ));
                }

                return list;
            }

            Assert.Equal(1, blockChain.Count);
            var txs = GenerateTransactions(5).OrderBy(tx => tx.Id).ToList();
            var preEvalBlock1 = new BlockContent(
                new BlockMetadata(
                    index: 1,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: null),
                transactions: txs).Propose();
            Block block1 = EvaluateAndSign(blockChain, preEvalBlock1, adminPrivateKey);
            blockChain.Append(block1, GenerateBlockCommit(block1, adminPrivateKey));
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));
            txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock2 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: GenerateBlockCommit(blockChain.Tip, adminPrivateKey)),
                transactions: txs).Propose();
            Block block2 = EvaluateAndSign(blockChain, preEvalBlock2, adminPrivateKey);
            blockChain.Append(block2, GenerateBlockCommit(block2, adminPrivateKey));
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block2.Hash));
            txs = GenerateTransactions(11).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock3 = new BlockContent(
                new BlockMetadata(
                    index: 3,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: GenerateBlockCommit(blockChain.Tip, adminPrivateKey)),
                transactions: txs).Propose();
            Block block3 = EvaluateAndSign(blockChain, preEvalBlock3, adminPrivateKey);
            Assert.Throws<InvalidBlockTxCountException>(
                () => blockChain.Append(block3, GenerateBlockCommit(block3, adminPrivateKey)));
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
                maxTransactionsBytesPolicy: null,
                minTransactionsPerBlockPolicy: null,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(0, null, null, 10)),
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy
                    .Default
                    .Add(new SpannedSubPolicy<int>(2, null, null, 5)));
            IStagePolicy<PolymorphicAction<ActionBase>> stagePolicy =
                new VolatileStagePolicy<PolymorphicAction<ActionBase>>();
            Block genesis =
                MakeGenesisBlock(
                    adminPublicKey.ToAddress(),
                    ImmutableHashSet<Address>.Empty,
                    initialValidators: new Dictionary<PublicKey, BigInteger>
                    { { adminPrivateKey.PublicKey, BigInteger.One } });

            using var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var blockChain = BlockChain<PolymorphicAction<ActionBase>>.Create(
                policy,
                stagePolicy,
                store,
                stateStore,
                genesis,
                new ActionEvaluator(
                    policyBlockActionGetter: _ => policy.BlockAction,
                    blockChainStates: new BlockChainStates(store, stateStore),
                    actionTypeLoader: new SingleActionLoader(typeof(PolymorphicAction<ActionBase>)),
                    feeCalculator: null
                )
            );

            int nonce = 0;
            List<Transaction> GenerateTransactions(int count)
            {
                var list = new List<Transaction>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(Transaction.Create(
                        nonce++,
                        adminPrivateKey,
                        genesis.Hash,
                        new PolymorphicAction<ActionBase>[] { }
                    ));
                }

                return list;
            }

            Assert.Equal(1, blockChain.Count);
            var txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock1 = new BlockContent(
                new BlockMetadata(
                    index: 1,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: null),
                transactions: txs).Propose();
            Block block1 = EvaluateAndSign(blockChain, preEvalBlock1, adminPrivateKey);

            // Should be fine since policy hasn't kicked in yet.
            blockChain.Append(block1, GenerateBlockCommit(block1, adminPrivateKey));
            Assert.Equal(2, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block1.Hash));

            txs = GenerateTransactions(10).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock2 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: GenerateBlockCommit(blockChain.Tip, adminPrivateKey)),
                transactions: txs).Propose();
            Block block2 = EvaluateAndSign(blockChain, preEvalBlock2, adminPrivateKey);

            // Subpolicy kicks in.
            Assert.Throws<InvalidBlockTxCountPerSignerException>(
                () => blockChain.Append(block2, GenerateBlockCommit(block2, adminPrivateKey)));
            Assert.Equal(2, blockChain.Count);
            Assert.False(blockChain.ContainsBlock(block2.Hash));
            // Since failed, roll back nonce.
            nonce -= 10;

            // Limit should also pass.
            txs = GenerateTransactions(5).OrderBy(tx => tx.Id).ToList();
            PreEvaluationBlock preEvalBlock3 = new BlockContent(
                new BlockMetadata(
                    index: 2,
                    timestamp: DateTimeOffset.MinValue,
                    publicKey: adminPublicKey,
                    previousHash: blockChain.Tip.Hash,
                    txHash: BlockContent.DeriveTxHash(txs),
                    lastCommit: GenerateBlockCommit(blockChain.Tip, adminPrivateKey)),
                transactions: txs).Propose();
            Block block3 = EvaluateAndSign(blockChain, preEvalBlock3, adminPrivateKey);
            blockChain.Append(block3, GenerateBlockCommit(block3, adminPrivateKey));
            Assert.Equal(3, blockChain.Count);
            Assert.True(blockChain.ContainsBlock(block3.Hash));
        }

        private BlockCommit GenerateBlockCommit(Block block, PrivateKey key)
        {
            PrivateKey privateKey = key;
            return block.Index != 0
                ? new BlockCommit(
                    block.Index,
                    0,
                    block.Hash,
                    ImmutableArray<Vote>.Empty.Add(new VoteMetadata(
                        block.Index,
                        0,
                        block.Hash,
                        DateTimeOffset.UtcNow,
                        privateKey.PublicKey,
                        VoteFlag.PreCommit).Sign(privateKey)))
                : null;
        }

        private Block MakeGenesisBlock(
            Address adminAddress,
            IImmutableSet<Address> activatedAddresses,
            AuthorizedMinersState authorizedMinersState = null,
            Dictionary<PublicKey, BigInteger> initialValidators = null,
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
            return BlockHelper.ProposeGenesisBlock(
                sheets,
                new GoldDistribution[0],
                pendingActivations,
                new AdminState(adminAddress, 1500000),
                authorizedMinersState: authorizedMinersState,
                activatedAccounts: activatedAddresses,
                initialValidators: initialValidators,
                isActivateAdminAddress: false,
                credits: null,
                privateKey: _privateKey,
                timestamp: timestamp ?? DateTimeOffset.MinValue);
        }

        private Block EvaluateAndSign(
            BlockChain<PolymorphicAction<ActionBase>> blockChain,
            PreEvaluationBlock preEvaluationBlock,
            PrivateKey privateKey
        )
        {
            var stateRootHash = blockChain.DetermineBlockStateRootHash(preEvaluationBlock, out _);
            return preEvaluationBlock.Sign(privateKey, stateRootHash);
        }
    }
}
