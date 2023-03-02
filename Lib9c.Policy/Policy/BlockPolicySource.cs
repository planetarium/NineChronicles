using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Renderers;
using Libplanet.Blocks;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Tx;
using Libplanet;
using Libplanet.Blockchain.Renderers;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Serilog;
using Serilog.Events;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
using Libplanet.Action;
using Lib9c.Abstractions;
using System.Reflection;

#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
#endif

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Model;
#endif

namespace Nekoyume.BlockChain.Policy
{
    public partial class BlockPolicySource
    {
        public const long MinimumDifficulty = 5_000_000;

        public const long DifficultyStability = 2048;

        /// <summary>
        /// Last index in which restriction will apply.
        /// </summary>
        public const long AuthorizedMinersPolicyEndIndex = 5_716_957;

        public const long AuthorizedMinersPolicyInterval = 50;

        public const int MaxTransactionsPerBlock = 100;

        public const long PermissionedMiningStartIndex = 2_225_500;

        public static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        public static readonly ImmutableHashSet<Address> AuthorizedMiners = new Address[]
        {
            new Address("ab1dce17dCE1Db1424BB833Af6cC087cd4F5CB6d"),
            new Address("3217f757064Cd91CAba40a8eF3851F4a9e5b4985"),
            new Address("474CB59Dea21159CeFcC828b30a8D864e0b94a6B"),
            new Address("636d187B4d434244A92B65B06B5e7da14b3810A9"),
        }.ToImmutableHashSet();

        private readonly IActionTypeLoader _actionTypeLoader;

        // FIXME: Why does BlockPolicySource have renderers?
        public readonly ActionRenderer ActionRenderer = new ActionRenderer();

        // FIXME: Why does BlockPolicySource have renderers?
        public readonly BlockRenderer BlockRenderer = new BlockRenderer();

        // FIXME: Why does BlockPolicySource have renderers?
        public readonly LoggedActionRenderer<NCAction> LoggedActionRenderer;

        // FIXME: Why does BlockPolicySource have renderers?
        public readonly LoggedRenderer<NCAction> LoggedBlockRenderer;

        public BlockPolicySource(
            ILogger logger,
            LogEventLevel logEventLevel = LogEventLevel.Verbose,
            IActionTypeLoader actionTypeLoader = null)
        {
            _actionTypeLoader = actionTypeLoader ?? new StaticActionTypeLoader(
                Assembly.GetEntryAssembly() is Assembly entryAssembly
                    ? new[] { typeof(ActionBase).Assembly, entryAssembly }
                    : new[] { typeof(ActionBase).Assembly },
                typeof(ActionBase)
            );

            LoggedActionRenderer =
                new LoggedActionRenderer<NCAction>(ActionRenderer, logger, logEventLevel);

            LoggedBlockRenderer =
                new LoggedRenderer<NCAction>(BlockRenderer, logger, logEventLevel);
        }

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-main deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetPolicy() =>
            GetPolicy(
                minimumDifficulty: MinimumDifficulty,
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Mainnet,
                permissionedMinersPolicy: PermissionedMinersPolicy.Mainnet,
                minBlockProtocolVersionPolicy: MinBlockProtocolVersionPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-internal deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetInternalPolicy() =>
            GetPolicy(
                minimumDifficulty: MinimumDifficulty,
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Internal,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Internal,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Mainnet,
                permissionedMinersPolicy: PermissionedMinersPolicy.Mainnet,
                minBlockProtocolVersionPolicy: MinBlockProtocolVersionPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-permanent-test deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetPermanentPolicy() =>
            GetPolicy(
                minimumDifficulty: DifficultyStability,
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Permanent,
                permissionedMinersPolicy: PermissionedMinersPolicy.Permanent,
                minBlockProtocolVersionPolicy: MinBlockProtocolVersionPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance identical to the one deployed
        /// except with lower minimum difficulty for faster testing and benchmarking.
        /// </summary>
        public IBlockPolicy<NCAction> GetTestPolicy() =>
            GetPolicy(
                minimumDifficulty: DifficultyStability,
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Mainnet,
                permissionedMinersPolicy: PermissionedMinersPolicy.Mainnet,
                minBlockProtocolVersionPolicy: MinBlockProtocolVersionPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for networks
        /// with default options, without authorized mining and permissioned mining.
        /// </summary>
        public IBlockPolicy<NCAction> GetDefaultPolicy() =>
            GetPolicy(
                minimumDifficulty: DifficultyStability,
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Default,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Default,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Default,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Default,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Default,
                permissionedMinersPolicy: PermissionedMinersPolicy.Default,
                minBlockProtocolVersionPolicy: MinBlockProtocolVersionPolicy.Default);

        /// <summary>
        /// Gets a <see cref="BlockPolicy"/> constructed from given parameters.
        /// </summary>
        /// <param name="minimumDifficulty">The minimum difficulty that a <see cref="Block{T}"/>
        /// can have.  This is ignored for genesis blocks.</param>
        /// <param name="minTransactionsPerBlockPolicy">Used for minimum number of transactions
        /// required per block.</param>
        /// <param name="maxTransactionsPerBlockPolicy">The maximum number of
        /// <see cref="Transaction{T}"/>s that a <see cref="Block{T}"/> can have.</param>
        /// <param name="maxTransactionsPerSignerPerBlockPolicy">The maximum number of
        /// <see cref="Transaction{T}"/>s from a single miner that a <see cref="Block{T}"/>
        /// can have.</param>
        /// <param name="authorizedMinersPolicy">Used for authorized mining.</param>
        /// <param name="permissionedMinersPolicy">Used for permissioned mining.</param>
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy<NCAction> GetPolicy(
            long minimumDifficulty,
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> permissionedMinersPolicy,
            IVariableSubPolicy<int> minBlockProtocolVersionPolicy)
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");
             return new DebugPolicy(data.BlockDifficulty);
#else
            maxTransactionsBytesPolicy = maxTransactionsBytesPolicy
                ?? MaxTransactionsBytesPolicy.Default;
            minTransactionsPerBlockPolicy = minTransactionsPerBlockPolicy
                ?? MinTransactionsPerBlockPolicy.Default;
            maxTransactionsPerBlockPolicy = maxTransactionsPerBlockPolicy
                ?? MaxTransactionsPerBlockPolicy.Default;
            maxTransactionsPerSignerPerBlockPolicy = maxTransactionsPerSignerPerBlockPolicy
                ?? MaxTransactionsPerSignerPerBlockPolicy.Default;
            authorizedMinersPolicy = authorizedMinersPolicy
                ?? AuthorizedMinersPolicy.Default;
            permissionedMinersPolicy = permissionedMinersPolicy
                ?? PermissionedMinersPolicy.Default;
            minBlockProtocolVersionPolicy = minBlockProtocolVersionPolicy
                ?? MinBlockProtocolVersionPolicy.Default;

            // FIXME: Ad hoc solution to poorly defined tx validity.
            ImmutableHashSet<Address> allAuthorizedMiners =
                authorizedMinersPolicy.SpannedSubPolicies
                    .Select(spannedSubPolicy => spannedSubPolicy.Value)
#pragma warning disable LAA1002
                    .Aggregate(
                        authorizedMinersPolicy.DefaultValue,
                        (union, next) => union.Union(next));
#pragma warning restore LAA1002

            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException> validateNextBlockTx =
                (blockChain, transaction) => ValidateNextBlockTxRaw(
                    blockChain, _actionTypeLoader, transaction, allAuthorizedMiners);
            Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException> validateNextBlock =
                (blockChain, block) => ValidateNextBlockRaw(
                    blockChain,
                    block,
                    maxTransactionsBytesPolicy,
                    minTransactionsPerBlockPolicy,
                    maxTransactionsPerBlockPolicy,
                    maxTransactionsPerSignerPerBlockPolicy,
                    authorizedMinersPolicy,
                    permissionedMinersPolicy,
                    minBlockProtocolVersionPolicy);
            Func<BlockChain<NCAction>, long> getNextBlockDifficulty = blockChain =>
                GetNextBlockDifficultyRaw(
                    blockChain,
                    BlockInterval,
                    DifficultyStability,
                    minimumDifficulty,
                    authorizedMinersPolicy,
                    defaultAlgorithm: chain => DifficultyAdjustment<NCAction>.BaseAlgorithm(
                        chain, BlockInterval, DifficultyStability, minimumDifficulty));
            Func<Address, long, bool> isAllowedToMine = (address, index) => IsAllowedToMineRaw(
                address,
                index,
                authorizedMinersPolicy,
                permissionedMinersPolicy);

            // FIXME: Slight inconsistency due to pre-existing delegate.
            return new BlockPolicy(
                new RewardGold(),
                blockInterval: BlockInterval,
                difficultyStability: DifficultyStability,
                minimumDifficulty: minimumDifficulty,
                canonicalChainComparer: new TotalDifficultyComparer(),
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxTransactionsBytes: maxTransactionsBytesPolicy.Getter,
                getMinTransactionsPerBlock: minTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerBlock: maxTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerSignerPerBlock: maxTransactionsPerSignerPerBlockPolicy.Getter,
                getNextBlockDifficulty: getNextBlockDifficulty,
                isAllowedToMine: isAllowedToMine,
                getMinBlockProtocolVersion: minBlockProtocolVersionPolicy.Getter);
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        internal static TxPolicyViolationException ValidateNextBlockTxRaw(
            BlockChain<NCAction> blockChain,
            IActionTypeLoader actionTypeLoader,
            Transaction<NCAction> transaction,
            ImmutableHashSet<Address> allAuthorizedMiners)
        {
            // Avoid NRE when genesis block appended
            long index = blockChain.Count > 0 ? blockChain.Tip.Index + 1: 0;

            if (((ITransaction)transaction).CustomActions?.Count > 1)
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} has too many actions: " +
                    $"{((ITransaction)transaction).CustomActions?.Count}",
                    transaction.Id);
            }
            else if (IsObsolete(transaction, actionTypeLoader, index))
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} is obsolete.",
                    transaction.Id);
            }

            try
            {
                // Check if it is a no-op transaction to prove it's made by the authorized miner.
                if (IsAuthorizedMinerTransactionRaw(transaction, allAuthorizedMiners))
                {
                    // FIXME: This works under a strong assumption that any miner that was ever
                    // in a set of authorized miners can only create transactions without
                    // any actions.
                    return ((ITransaction)transaction).CustomActions?.Any() is true
                        ? new TxPolicyViolationException(
                            $"Transaction {transaction.Id} by an authorized miner should not " +
                            $"have any action: {((ITransaction)transaction).CustomActions?.Count}",
                            transaction.Id)
                        : null;
                }

                var actionTypes = actionTypeLoader.Load(new ActionTypeLoaderContext(index));
                // Check ActivateAccount
                if (((ITransaction)transaction).CustomActions is { } customActions &&
                    customActions.Count == 1 &&
                    customActions.First() is Dictionary dictionary &&
                    dictionary.TryGetValue((Text)"type_id", out IValue typeIdValue) &&
                    typeIdValue is Text typeId &&
                    (typeId == "activate_account2" || typeId == "activate_account"))
                {
                    if (!(dictionary.TryGetValue((Text)"values", out IValue valuesValue) &&
                          valuesValue is Dictionary values))
                    {
                        return new TxPolicyViolationException(
                            $"Transaction {transaction.Id} has an invalid action.",
                            transaction.Id);
                    }

                    IAction action = (IAction)Activator.CreateInstance(actionTypes[typeId]);
                    if (!(action is IActivateAccount activateAccount))
                    {
                        return new TxPolicyViolationException(
                            $"Transaction {transaction.Id} has an invalid action.",
                            transaction.Id);
                    }
                    action.LoadPlainValue(values);

                    return transaction.Nonce == 0 &&
                           blockChain.GetState(activateAccount.PendingAddress) is Dictionary rawPending &&
                           new PendingActivationState(rawPending).Verify(activateAccount.Signature)
                        ? null
                        : new TxPolicyViolationException(
                            $"Transaction {transaction.Id} has an invalid activate action.",
                            transaction.Id);
                }

                // Check admin
                if (IsAdminTransaction(blockChain, transaction))
                {
                    return null;
                }

                switch (blockChain.GetState(transaction.Signer.Derive(ActivationKey.DeriveKey)))
                {
                    case null:
                        // Fallback for pre-migration.
                        if (blockChain.GetState(ActivatedAccountsState.Address)
                            is Dictionary asDict)
                        {
                            IImmutableSet<Address> activatedAccounts =
                                new ActivatedAccountsState(asDict).Accounts;
                            return !activatedAccounts.Any() ||
                                activatedAccounts.Contains(transaction.Signer)
                                ? null
                                : new TxPolicyViolationException(
                                    $"Transaction {transaction.Id} is by a signer " +
                                    $"without account activation: {transaction.Signer}",
                                    transaction.Id);
                        }
                        return null;
                    case Bencodex.Types.Boolean _:
                        return null;
                }

                return null;
            }
            catch (InvalidSignatureException)
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} has invalid signautre.",
                    transaction.Id);
            }
            catch (IncompleteBlockStatesException)
            {
                // It can be caused during `Swarm<T>.PreloadAsync()` because it doesn't fill its
                // state right away...
                // FIXME: It should be removed after fix that Libplanet fills its state on IBD.
                // See also: https://github.com/planetarium/lib9c/pull/151#discussion_r506039478
                return null;
            }

            return null;
        }

        internal static BlockPolicyViolationException ValidateNextBlockRaw(
            BlockChain<NCAction> blockChain,
            Block<NCAction> nextBlock,
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> permissionedMinersPolicy,
            IVariableSubPolicy<int> minBlockProtocolVersionPolicy)
        {
            if (ValidateBlockProtocolVersionRaw(
                nextBlock,
                minBlockProtocolVersionPolicy) is BlockPolicyViolationException bpve)
            {
                return bpve;
            }
            else if (ValidateTransactionsBytesRaw(
                nextBlock,
                maxTransactionsBytesPolicy) is InvalidBlockBytesLengthException ibble)
            {
                return ibble;
            }
            else if (ValidateTxCountPerBlockRaw(
                nextBlock,
                minTransactionsPerBlockPolicy,
                maxTransactionsPerBlockPolicy) is InvalidBlockTxCountException ibtce)
            {
                return ibtce;
            }
            else if (ValidateTxCountPerSignerPerBlockRaw(
                nextBlock,
                maxTransactionsPerSignerPerBlockPolicy) is InvalidBlockTxCountPerSignerException ibtcpse)
            {
                return ibtcpse;
            }
            else
            {
                if (nextBlock.Index == 0)
                {
                    return null;
                }
                else if (authorizedMinersPolicy.IsTargetIndex(nextBlock.Index))
                {
                    return ValidateMinerAuthorityRaw(
                        nextBlock,
                        authorizedMinersPolicy);
                }
                else if (permissionedMinersPolicy.IsTargetIndex(nextBlock.Index))
                {
                    return ValidateMinerPermissionRaw(
                        nextBlock,
                        permissionedMinersPolicy);
                }
            }

            return null;
        }

        // FIXME: Although the intention is to use a slight variant of the algorithm provided,
        // this allows a wildly different implementation for special cases.
        internal static long GetNextBlockDifficultyRaw(
            BlockChain<NCAction> blockChain,
            TimeSpan targetBlockInterval,
            long difficultyStability,
            long minimumDifficulty,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy,
            Func<BlockChain<NCAction>, long> defaultAlgorithm)
        {
            long index = blockChain.Count;
            Func<long, bool> isAuthorizedMiningIndex = authorizedMinersPolicy.IsTargetIndex;

            // FIXME: Uninstantiated blockChain can be passed as an argument.
            // Until this is fixed, it is crucial block index is checked first.
            // Authorized minor validity is only checked for certain indices.
            if (index < 0)
            {
                throw new InvalidBlockIndexException(
                    $"Value of {nameof(index)} must be non-negative: {index}");
            }
            else if (index <= 1)
            {
                return index == 0 ? 0 : minimumDifficulty;
            }
            else if (isAuthorizedMiningIndex(index))
            {
                return minimumDifficulty;
            }
            else
            {
                long prevIndex = !isAuthorizedMiningIndex(index - 1)
                    ? index - 1
                    : index - 2;
                long prevPrevIndex = !isAuthorizedMiningIndex(prevIndex - 1)
                    ? prevIndex - 1
                    : prevIndex - 2;

                // Arbitrary condition not strictly necessary, but already hardcoded.
                if (prevPrevIndex <= 1)
                {
                    return minimumDifficulty;
                }
                // Blocks with index, prevIndex, and prevPrevIndex are all
                // non-authorized mining blocks.
                else if (prevPrevIndex == index - 2)
                {
                    return defaultAlgorithm(blockChain);
                }
                // At least one of previous blocks involved is authorized mining block.
                // This can happen if two or more consecutive blocks are authorized mining blocks.
                else if (isAuthorizedMiningIndex(prevIndex)
                    || isAuthorizedMiningIndex(prevPrevIndex))
                {
                    return minimumDifficulty;
                }
                else
                {
                    Block<NCAction> prevBlock = blockChain[prevIndex];
                    Block<NCAction> prevPrevBlock = blockChain[prevPrevIndex];
                    TimeSpan prevTimeDiff = prevBlock.Timestamp - prevPrevBlock.Timestamp;
                    const long minimumAdjustmentMultiplier = -99;

                    long adjustmentMultiplier = Math.Max(
                        1 - ((long)prevTimeDiff.TotalMilliseconds /
                            (long)targetBlockInterval.TotalMilliseconds),
                        minimumAdjustmentMultiplier);
                    long difficultyAdjustment =
                        prevBlock.Difficulty / difficultyStability * adjustmentMultiplier;

                    long nextDifficulty = Math.Max(
                        prevBlock.Difficulty + difficultyAdjustment, minimumDifficulty);

                    return nextDifficulty;
                }
            }

        }

        private class ActionTypeLoaderContext : IActionTypeLoaderContext
        {
            public ActionTypeLoaderContext(long index)
            {
                Index = index;
            }

            public long Index { get; }
        }
    }
}
