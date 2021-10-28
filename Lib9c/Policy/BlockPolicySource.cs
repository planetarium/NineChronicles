using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Renderer;
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
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
#endif
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain.Policy
{
    public partial class BlockPolicySource
    {
        public const long MinimumDifficulty = 5_000_000;

        public const long DifficultyStability = 2048;

        // FIXME: We should adjust this value after resolving
        // https://github.com/planetarium/NineChronicles/issues/777
        // Previous value is 100 kb (until v100080)
        public const int MaxBlockBytes = 1024 * 1024 * 10; // 10 Mib

        // Note: The genesis block of 9c-main net weighs 11,085,640 B (11 MiB).
        public const int MaxGenesisBytes = 1024 * 1024 * 15; // 15 MiB

        /// <summary>
        /// Last index in which restriction will apply.
        /// </summary>
        public const long AuthorizedMinersPolicyEndIndex = 3_153_600;

        public const long AuthorizedMinersPolicyInterval = 50;

        /// <summary>
        /// First index in which restriction will apply.
        /// </summary>
        public const long AuthorizedMiningNoOpTxRequiredStartIndex = 1_200_001;

        /// <summary>
        /// First index in which restriction will apply.
        /// </summary>
        public const long MinTransactionsPerBlockStartIndex = 2_173_701;

        public const int MinTransactionsPerBlock = 1;

        public const int MaxTransactionsPerBlock = 100;

        // FIXME: Should be finalized before release.
        public const long MaxTransactionsPerSignerPerBlockStartIndex = 3_000_001;

        public const int MaxTransactionsPerSignerPerBlock = 4;

        public const long V100080ObsoleteIndex = 2_448_000;

        public const long V100081ObsoleteIndex = 2_550_000;

        // FIXME: Should be finalized before release.
        // current: 2021. 10. 21. pm 04:00 KST // 2,576,828
        // target: 2021. 10. 28. am 11:00 KST
        // seconds per block: 12
        public const long V100083ObsoleteIndex = 2_625_728;

        public const long V100084ObsoleteIndex = 2_700_000;

        public const long PermissionedMiningHardcodedIndex = 2_225_500;

        public const long PermissionedMiningStartIndex = 2_225_500;


        public static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        public static readonly ImmutableHashSet<Address> AuthorizedMiners = new Address[]
        {
            new Address("ab1dce17dCE1Db1424BB833Af6cC087cd4F5CB6d"),
            new Address("3217f757064Cd91CAba40a8eF3851F4a9e5b4985"),
            new Address("474CB59Dea21159CeFcC828b30a8D864e0b94a6B"),
            new Address("636d187B4d434244A92B65B06B5e7da14b3810A9"),
        }.ToImmutableHashSet();

        public readonly ActionRenderer ActionRenderer = new ActionRenderer();

        public readonly BlockRenderer BlockRenderer = new BlockRenderer();

        public readonly LoggedActionRenderer<NCAction> LoggedActionRenderer;

        public readonly LoggedRenderer<NCAction> LoggedBlockRenderer;

        public BlockPolicySource(
            ILogger logger,
            LogEventLevel logEventLevel = LogEventLevel.Verbose)
        {
            LoggedActionRenderer =
                new LoggedActionRenderer<NCAction>(ActionRenderer, logger, logEventLevel);

            LoggedBlockRenderer =
                new LoggedRenderer<NCAction>(BlockRenderer, logger, logEventLevel);
        }

        [Obsolete("Left for temporary old API compliance.")]
        public IBlockPolicy<NCAction> GetPolicy(
            long minimumDifficulty,
            int maxTransactionsPerBlock) =>
            GetPolicy(
                minimumDifficulty: minimumDifficulty,
                maxBlockBytesPolicy: MaxBlockBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Default.Add(
                    new SpannedSubPolicy<int>(
                        startIndex: 0,
                        value: maxTransactionsPerBlock)),
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Mainnet,
                authorizedMiningNoOpTxRequiredPolicy: AuthorizedMiningNoOpTxRequiredPolicy.Mainnet,
                permissionedMinersPolicy: PermissionedMinersPolicy.Mainnet);

        public IBlockPolicy<NCAction> GetPolicy() =>
            GetPolicy(
                minimumDifficulty: MinimumDifficulty,
                maxBlockBytesPolicy: MaxBlockBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet,
                authorizedMinersPolicy: AuthorizedMinersPolicy.Mainnet,
                authorizedMiningNoOpTxRequiredPolicy: AuthorizedMiningNoOpTxRequiredPolicy.Mainnet,
                permissionedMinersPolicy: PermissionedMinersPolicy.Mainnet);

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
        /// <param name="authorizedMiningNoOpTxRequiredPolicy">Used for no-op tx proof check
        /// for authorized mining.</param>
        /// <param name="permissionedMinersPolicy">Used for permissioned mining.</param>
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy<NCAction> GetPolicy(
            long minimumDifficulty,
            IVariableSubPolicy<int> maxBlockBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy,
            IVariableSubPolicy<bool> authorizedMiningNoOpTxRequiredPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> permissionedMinersPolicy)
        {
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            maxBlockBytesPolicy = maxBlockBytesPolicy
                ?? MaxBlockBytesPolicy.Default;
            minTransactionsPerBlockPolicy = minTransactionsPerBlockPolicy
                ?? MinTransactionsPerBlockPolicy.Default;
            maxTransactionsPerBlockPolicy = maxTransactionsPerBlockPolicy
                ?? MaxTransactionsPerBlockPolicy.Default;
            maxTransactionsPerSignerPerBlockPolicy = maxTransactionsPerSignerPerBlockPolicy
                ?? MaxTransactionsPerSignerPerBlockPolicy.Default;
            authorizedMinersPolicy = authorizedMinersPolicy
                ?? AuthorizedMinersPolicy.Default;
            authorizedMiningNoOpTxRequiredPolicy = authorizedMiningNoOpTxRequiredPolicy
                ?? AuthorizedMiningNoOpTxRequiredPolicy.Default;
            permissionedMinersPolicy = permissionedMinersPolicy
                ?? PermissionedMinersPolicy.Default;

            // FIXME: Slight inconsistency due to pre-existing delegate.
            HashAlgorithmGetter getHashAlgorithmType =
                index => HashAlgorithmTypePolicy.Mainnet.Getter(index);

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
                    blockChain, transaction, allAuthorizedMiners);
            Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException> validateNextBlock =
                (blockChain, block) => ValidateNextBlockRaw(
                    blockChain,
                    block,
                    minTransactionsPerBlockPolicy,
                    maxTransactionsPerBlockPolicy,
                    maxTransactionsPerSignerPerBlockPolicy,
                    authorizedMinersPolicy,
                    authorizedMiningNoOpTxRequiredPolicy,
                    permissionedMinersPolicy);
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

            return new BlockPolicy(
                new RewardGold(),
                blockInterval: BlockInterval,
                difficultyStability: DifficultyStability,
                minimumDifficulty: minimumDifficulty,
                canonicalChainComparer: new TotalDifficultyComparer(),
                hashAlgorithmGetter: getHashAlgorithmType,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxBlockBytes: maxBlockBytesPolicy.Getter,
                getMinTransactionsPerBlock: minTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerBlock: maxTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerSignerPerBlock: maxTransactionsPerSignerPerBlockPolicy.Getter,
                getNextBlockDifficulty: getNextBlockDifficulty,
                isAllowedToMine: isAllowedToMine);
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        internal static TxPolicyViolationException ValidateNextBlockTxRaw(
            BlockChain<NCAction> blockChain,
            Transaction<NCAction> transaction,
            ImmutableHashSet<Address> allAuthorizedMiners)
        {
            // Avoid NRE when genesis block appended
            // Here, index is the index of a prospective block that transaction
            // will be included.
            long index = blockChain.Count > 0 ? blockChain.Tip.Index : 0;

            if (transaction.Actions.Count > 1)
            {
                return new TxPolicyViolationException(
                    transaction.Id,
                    $"Transaction {transaction.Id} has too many actions: " +
                    $"{transaction.Actions.Count}");
            }
            else if (IsObsolete(transaction, index))
            {
                return new TxPolicyViolationException(
                    transaction.Id,
                    $"Transaction {transaction.Id} is obsolete.");
            }

            try
            {
                // Check if it is a no-op transaction to prove it's made by the authorized miner.
                if (IsAuthorizedMinerTransactionRaw(transaction, allAuthorizedMiners))
                {
                    // FIXME: This works under a strong assumption that any miner that was ever
                    // in a set of authorized miners can only create transactions without
                    // any actions.
                    return transaction.Actions.Any()
                        ? new TxPolicyViolationException(
                            transaction.Id,
                            $"Transaction {transaction.Id} by an authorized miner should not " +
                            $"have any action: {transaction.Actions.Count}")
                        : null;
                }

                // Check ActivateAccount
                if (transaction.Actions.Count == 1 &&
                    transaction.Actions.First().InnerAction is IActivateAction aa)
                {
                    return blockChain.GetState(aa.GetPendingAddress()) is Dictionary rawPending &&
                        new PendingActivationState(rawPending).Verify(aa.GetSignature())
                        ? null
                        : new TxPolicyViolationException(
                            transaction.Id,
                            $"Transaction {transaction.Id} has an invalid activate action.");
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
                                    transaction.Id,
                                    $"Transaction {transaction.Id} is by a signer " +
                                    $"without account activation: {transaction.Signer}");
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
                    transaction.Id,
                    $"Transaction {transaction.Id} has invalid signautre.");
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
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> authorizedMinersPolicy,
            IVariableSubPolicy<bool> authorizedMiningNoOpTxRequiredPolicy,
            IVariableSubPolicy<ImmutableHashSet<Address>> permissionedMinersPolicy)
        {
            // FIXME: Tx count validation should be done in libplanet, not here.
            // Should be removed once libplanet is updated.
            if (ValidateTxCountPerBlockRaw(
                nextBlock,
                minTransactionsPerBlockPolicy,
                maxTransactionsPerBlockPolicy,
                maxTransactionsPerSignerPerBlockPolicy) is BlockPolicyViolationException bpve)
            {
                return bpve;
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
                        authorizedMinersPolicy,
                        authorizedMiningNoOpTxRequiredPolicy);
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
    }
}
