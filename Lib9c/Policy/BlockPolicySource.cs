using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
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
        public const int DifficultyStability = 2048;

        // Note: The heaviest block of 9c-main (except for the genesis) weighs 58,408 B (58 KiB).
        public const int MaxBlockBytes = 1024 * 100; // 100 KiB

        // Note: The genesis block of 9c-main net weighs 11,085,640 B (11 MiB).
        public const int MaxGenesisBytes = 1024 * 1024 * 15; // 15 MiB

        /// <summary>
        /// Starting point in which Validate restriction will apply.
        /// </summary>
        public const long ValidateMinerAuthorityNoOpTxHardcodedIndex = 1_200_001;

        /// <summary>
        /// Starting point in which MinTransactionsPerBlock restriction will apply.
        /// </summary>
        public const long MinTransactionsPerBlockHardcodedIndex = 2_173_701;

        public const int MinTransactionsPerBlock = 1;

        // FIXME: Should be finalized before release.
        public const long MaxTransactionsPerSignerPerBlockHardcodedIndex = 3_000_001;

        // FIXME: Should be finalized before release.
        public const int MaxTransactionsPerSignerPerBlock = 4;

        public readonly Dictionary<long, HashAlgorithmType> HashAlgorithmTable =
            new Dictionary<long, HashAlgorithmType> { [0] = HashAlgorithmType.Of<SHA256>() };

        public readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        public readonly ActionRenderer ActionRenderer = new ActionRenderer();

        public readonly BlockRenderer BlockRenderer = new BlockRenderer();

        public readonly LoggedActionRenderer<NCAction> LoggedActionRenderer;

        public readonly LoggedRenderer<NCAction> LoggedBlockRenderer;

        public BlockPolicySource(ILogger logger, LogEventLevel logEventLevel = LogEventLevel.Verbose)
        {
            LoggedActionRenderer =
                new LoggedActionRenderer<NCAction>(ActionRenderer, logger, logEventLevel);

            LoggedBlockRenderer =
                new LoggedRenderer<NCAction>(BlockRenderer, logger, logEventLevel);
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public IBlockPolicy<NCAction> GetPolicy(int minimumDifficulty, int maxTransactionsPerBlock) =>
            GetPolicy(
                minimumDifficulty,
                maxTransactionsPerBlock,
                ignoreHardcodedPolicies: false,
                permissionedMiningPolicy: PermissionedMiningPolicy.Mainnet);

        /// <summary>
        /// Gets a <see cref="BlockPolicy"/> constructed from given parameters.
        /// </summary>
        /// <param name="minimumDifficulty">The minimum difficulty that a <see cref="Block{T}"/>
        /// can have.  This is ignored for genesis blocks.</param>
        /// <param name="maxTransactionsPerBlock">The maximum number of
        /// <see cref="Transaction{T}"/>s that a <see cref="Block{T}"/> can have.</param>
        /// <param name="ignoreHardcodedPolicies">
        /// <para>
        /// Whether to ignore or respect hardcoded policies.
        /// </para>
        /// <para>
        /// There are several policies where each policy only applies after its corresponding
        /// hardcoded index.  Turning on this option ignores these hard coded indices and
        /// applies said policies starting from index 0, the gensis.
        /// </para>
        /// <para>
        /// This is purely for unit testing and should be set to false for production.
        /// </para>
        /// </param>
        /// <param name="permissionedMiningPolicy">Used for permissioned mining.</param>
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy<NCAction> GetPolicy(
            int minimumDifficulty,
            int maxTransactionsPerBlock,
            bool ignoreHardcodedPolicies,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
#if UNITY_EDITOR
            return new Lib9c.DebugPolicy();
#else
            Func<BlockChain<NCAction>, Address, long, bool> isAllowedToMine =
                IsAllowedToMineFactory(
                    IsPermissionedMiningBlockIndexFactory(permissionedMiningPolicy),
                    IsPermissionedToMineFactory(permissionedMiningPolicy));

            return new BlockPolicy(
                new RewardGold(),
                blockInterval: BlockInterval,
                difficultyStability: DifficultyStability,
                minimumDifficulty: minimumDifficulty,
                permissionedMiningPolicy: permissionedMiningPolicy,
                canonicalChainComparer: new TotalDifficultyComparer(),
#pragma warning disable LAA1002
                hashAlgorithmGetter: HashAlgorithmTable.ToHashAlgorithmGetter(),
#pragma warning restore LAA1002
                validateNextBlockTx: ValidateNextBlockTx,
                validateNextBlock: ValidateNextBlockFactory(permissionedMiningPolicy, ignoreHardcodedPolicies),
                getMaxBlockBytes: GetMaxBlockBytes,
                getMinTransactionsPerBlock: GetMinTransactionsPerBlock,
                getMaxTransactionsPerBlock: GetMaxTransactionsPerBlockFactory(maxTransactionsPerBlock),
                getMaxTransactionsPerSignerPerBlock: GetMaxTransactionsPerSignerPerBlock,
                getAuthorizedMinersState: GetAuthorizedMinersState,
                isAllowedToMine: isAllowedToMine);
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        public static TxPolicyViolationException ValidateNextBlockTx(
            BlockChain<NCAction> blockChain,
            Transaction<NCAction> transaction)
        {
            // Avoid NRE when genesis block appended
            // Here, index is the index of a prospective block that transaction
            // will be included.
            long index = blockChain.Count > 0 ? blockChain.Tip.Index : 0;

            if (transaction.Actions.Count > 1)
            {
                return new TxPolicyViolationException(
                    transaction.Id,
                    $"Transaction {transaction.Id} has too many actions: "
                        + $"{transaction.Actions.Count}");
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
                if (IsAuthorizedMinerTransaction(blockChain, transaction))
                {
                    // The authorization proof has to have no actions at all.
                    return transaction.Actions.Any()
                        ? new TxPolicyViolationException(
                            transaction.Id,
                            $"Transaction {transaction.Id} by an authorized miner should not have "
                                + $"any action: {transaction.Actions.Count}")
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
                                    $"Transaction {transaction.Id} is by a signer "
                                        + $"without account activation: {transaction.Signer}");
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

        public static BlockPolicyViolationException ValidateNextBlockRaw(
            BlockChain<NCAction> blockChain,
            Block<NCAction> nextBlock,
            PermissionedMiningPolicy? permissionedMiningPolicy,
            bool ignoreHardcodedPolicies)
        {
            return ValidateTxCountPerBlockRaw(nextBlock, ignoreHardcodedPolicies)
                ?? ValidateMinerAuthorityRaw(blockChain, nextBlock, ignoreHardcodedPolicies)
                ?? ValidateMinerPermissionRaw(nextBlock, permissionedMiningPolicy, ignoreHardcodedPolicies);
        }

        public static Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException>
            ValidateNextBlockFactory(
                PermissionedMiningPolicy? permissionedMiningPolicy,
                bool ignoreHardcodedPolicies)
        {
            return (blockChain, nextBlock) =>
                ValidateNextBlockRaw(
                    blockChain,
                    nextBlock,
                    permissionedMiningPolicy,
                    ignoreHardcodedPolicies);
        }

        public static int GetMaxBlockBytes(long index)
        {
            return index > 0 ? MaxBlockBytes : MaxGenesisBytes;
        }

        public static int GetMinTransactionsPerBlock(long index)
        {
            return index >= MinTransactionsPerBlockHardcodedIndex
                ? MinTransactionsPerBlock
                : 0;
        }

        public static int GetMaxTransactionsPerBlockRaw(long index, int maxTransactionsPerBlock)
        {
            return maxTransactionsPerBlock;
        }

        public static Func<long, int> GetMaxTransactionsPerBlockFactory(
            int maxTransactionsPerBlock)
        {
            return index => GetMaxTransactionsPerBlockRaw(index, maxTransactionsPerBlock);
        }

        public static int GetMaxTransactionsPerSignerPerBlock(long index)
        {
            return index >= MaxTransactionsPerSignerPerBlockHardcodedIndex
                ? MaxTransactionsPerSignerPerBlock
                : int.MaxValue;
        }
    }
}
