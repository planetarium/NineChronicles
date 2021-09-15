using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
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
    public class BlockPolicySource
    {
        public const int DifficultyBoundDivisor = 2048;

        // Note: The heaviest block of 9c-main (except for the genesis) weighs 58,408 B (58 KiB).
        public const int MaxBlockBytes = 1024 * 100; // 100 KiB

        // Note: The genesis block of 9c-main net weighs 11,085,640 B (11 MiB).
        public const int MaxGenesisBytes = 1024 * 1024 * 15; // 15 MiB

        public const long V100073ObsoleteIndex = 3000000;

        // FIXME: Should be finalized before release.
        public const int maxTransactionsPerSignerPerBlockV100074 = 4;

        private static readonly Dictionary<long, HashAlgorithmType> _hashAlgorithmTable =
            new Dictionary<long, HashAlgorithmType> { [0] = HashAlgorithmType.Of<SHA256>() };

        private readonly TimeSpan _blockInterval = TimeSpan.FromSeconds(8);

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

        public IBlockPolicy<NCAction> GetPolicy(int minimumDifficulty, int maximumTransactions) =>
            GetPolicy(
                minimumDifficulty,
                maximumTransactions,
                ignoreHardcodedPolicies: false,
                permissionedMiningPolicy: PermissionedMiningPolicy.Mainnet);

        // FIXME 남은 설정들도 설정화 해야 할지도?
        internal IBlockPolicy<NCAction> GetPolicy(
            int minimumDifficulty,
            int maximumTransactions,
            bool ignoreHardcodedPolicies,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
#if UNITY_EDITOR
            return new Lib9c.DebugPolicy();
#else
            return new BlockPolicy(
                new RewardGold(),
                blockInterval: _blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: DifficultyBoundDivisor,
                ignoreHardcodedPolicies: ignoreHardcodedPolicies,
                permissionedMiningPolicy: permissionedMiningPolicy,
                canonicalChainComparer: new TotalDifficultyComparer(),
#pragma warning disable LAA1002
                hashAlgorithmGetter: _hashAlgorithmTable.ToHashAlgorithmGetter(),
#pragma warning restore LAA1002
                validateNextBlockTx: ValidateNextBlockTx,
                getMaxBlockBytes: (long index) => index > 0 ? MaxBlockBytes : MaxGenesisBytes,
                getMinTransactionsPerBlock: (long index) => 0,
                getMaxTransactionsPerBlock: (long index) => maximumTransactions,
                getMaxTransactionsPerSignerPerBlock: (long index) => index > V100073ObsoleteIndex
                    ? maxTransactionsPerSignerPerBlockV100074
                    : maximumTransactions);
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        public static bool IsObsolete(Transaction<NCAction> transaction, long blockIndex)
        {
            return transaction.Actions
                .Select(action => action.InnerAction.GetType())
                .Any(
                    at =>
                    at.IsDefined(typeof(ActionObsoleteAttribute), false) &&
                    at.GetCustomAttributes()
                        .OfType<ActionObsoleteAttribute>()
                        .FirstOrDefault()?.ObsoleteIndex < blockIndex
                );
        }

        public static bool IsAuthorizedMinerTransaction(
            BlockChain<NCAction> blockChain, Transaction<NCAction> transaction)
        {
            return blockChain.GetState(AuthorizedMinersState.Address) is Dictionary rawAms
                && new AuthorizedMinersState(rawAms).Miners.Contains(transaction.Signer);
        }

        public static bool IsAdminTransaction(
            BlockChain<NCAction> blockChain, Transaction<NCAction> transaction)
        {
            return blockChain.GetState(Addresses.Admin) is Dictionary rawAdmin
                && new AdminState(rawAdmin).AdminAddress.Equals(transaction.Signer);
        }

        private TxPolicyViolationException ValidateNextBlockTx(
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
                // FIXME It should be removed after fix that Libplanet fills its state on IBD.
                // See also: https://github.com/planetarium/lib9c/pull/151#discussion_r506039478
                return null;
            }

            return null;
        }
    }
}
