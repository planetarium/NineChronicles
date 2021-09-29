using System;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain.Policy
{
    // Collection of helper methods not directly used as a pluggable component.
    public partial class BlockPolicySource
    {
        internal static bool IsObsolete(Transaction<NCAction> transaction, long blockIndex)
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

        internal static bool IsAdminTransaction(
            BlockChain<NCAction> blockChain, Transaction<NCAction> transaction)
        {
            return GetAdminState(blockChain) is AdminState admin
                && admin.AdminAddress.Equals(transaction.Signer);
        }

        internal static bool IsAuthorizedMinerTransactionRaw(
            Transaction<NCAction> transaction, AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            if (authorizedMiningPolicy is AuthorizedMiningPolicy amp)
            {
                return amp.Miners.Contains(transaction.Signer);
            }
            else
            {
                return false;
            }
        }

        internal static Func<Transaction<NCAction>, bool> IsAuthorizedMinerTransactionFactory(
            AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            return transaction => IsAuthorizedMinerTransactionRaw(
                transaction, authorizedMiningPolicy);
        }

        internal static AdminState GetAdminState(
            BlockChain<NCAction> blockChain)
        {
            try
            {
                return blockChain.GetState(AdminState.Address) is Dictionary rawAdmin
                    ? new AdminState(rawAdmin)
                    : null;
            }
            catch (IncompleteBlockStatesException)
            {
                return null;
            }
        }

        // FIXME: Tx count validation should be done in libplanet, not here.
        // Should be removed once libplanet is updated.
        internal static BlockPolicyViolationException ValidateTxCountPerBlockRaw(
            Block<NCAction> block,
            MinTransactionsPerBlockPolicy? minTransactionsPerBlockPolicy)
        {
            // To prevent selfish mining, we define a consensus that blocks with no transactions
            // are not accepted starting from MinTransactionsPerBlockHardcodedIndex.
            return ValidateMinTransactionsPerBlockRaw(block, minTransactionsPerBlockPolicy);
        }

        internal static BlockPolicyViolationException ValidateMinTransactionsPerBlockRaw(
            Block<NCAction> block, MinTransactionsPerBlockPolicy? minTransactionsPerBlockPolicy)
        {
            if (block.Transactions.Count < GetMinTransactionsPerBlockRaw(block.Index, minTransactionsPerBlockPolicy))
            {
                return new BlockPolicyViolationException(
                    $"Block #{block.Index} {block.Hash} should include " +
                    $"at least {MinTransactionsPerBlock} transaction(s).");
            }

            return null;
        }

        internal static BlockPolicyViolationException ValidateMinerAuthorityRaw(
            Block<NCAction> block,
            AuthorizedMiningPolicy? authorizedMiningPolicy,
            AuthorizedMiningNoOpTxPolicy? authorizedMiningNoOpTxPolicy)
        {
            // For genesis block, any miner can mine.
            if (block.Index == 0)
            {
                return null;
            }
            // If not an authorized mining block index, any miner can mine.
            else if (!IsAuthorizedMiningBlockIndexRaw(block.Index, authorizedMiningPolicy))
            {
                return null;
            }
            // Otherwise, block's miner should be one of the authorized miners.
            else if (!IsAuthorizedToMineRaw(block.Miner, block.Index, authorizedMiningPolicy))
            {
                return new BlockPolicyViolationException(
                    $"The block #{block.Index} {block.Hash} is not mined by an authorized miner.");
            }
            else
            {
                return ValidateMinerAuthorityNoOpTxRaw(block, authorizedMiningNoOpTxPolicy);
            }
        }

        internal static Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException>
            ValidateMinerAuthorityFactory(
                AuthorizedMiningPolicy? authorizedMiningPolicy,
                AuthorizedMiningNoOpTxPolicy? authorizedMiningNoOpTxPolicy)
        {
            return (blockChain, block) =>
                ValidateMinerAuthorityRaw(
                    block,
                    authorizedMiningPolicy,
                    authorizedMiningNoOpTxPolicy);
        }

        internal static BlockPolicyViolationException ValidateMinerAuthorityNoOpTxRaw(
            Block<NCAction> block,
            AuthorizedMiningNoOpTxPolicy? authorizedMiningNoOpTxPolicy)
        {
            if (authorizedMiningNoOpTxPolicy is AuthorizedMiningNoOpTxPolicy amnotp)
            {
                if (amnotp.IsTargetBlockIndex(block.Index))
                {
                    // Authority is proven through a no-op transaction, i.e. a transaction
                    // with zero actions, starting from ValidateMinerAuthorityNoOpHardcodedIndex.
                    Transaction<NCAction>[] txs = block.Transactions.ToArray();
                    if (!txs.Any(tx => tx.Signer.Equals(block.Miner) && !tx.Actions.Any())
                            && block.ProtocolVersion > 0)
                    {
#if DEBUG
                        string debug =
                            "  Note that there " +
                            (txs.Length == 1
                                ? "is a transaction:"
                                : $"are {txs.Length} transactions:") +
                            txs.Select((tx, i) =>
                                    $"\n    {i}. {tx.Actions.Count} actions; signed by {tx.Signer}")
                                .Aggregate(string.Empty, (a, b) => a + b);
#else
                        const string debug = "";
#endif
                        return new BlockPolicyViolationException(
                            $"Block #{block.Index} {block.Hash}'s miner {block.Miner} should be "
                                + "proven by including a no-op transaction signed by "
                                + "the same authority." + debug);
                    }
                }
            }
            return null;
        }

        internal static BlockPolicyViolationException ValidateMinerPermissionRaw(
            Block<NCAction> block,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            Address miner = block.Miner;

            // If no permission policy is given, pass validation by default.
            if (!(permissionedMiningPolicy is PermissionedMiningPolicy pmp))
            {
                return null;
            }

            // Predicate for permission validity.
            if (!pmp.Miners.Contains(miner) || !block.Transactions.Any(t => t.Signer.Equals(miner)))
            {
                if (block.Index >= pmp.StartIndex)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} is not mined by a permissioned miner.");
                }
            }

            return null;
        }

        internal static Func<Block<NCAction>, BlockPolicyViolationException>
            ValidateMinerPermissionFactory(
                PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            return block => ValidateMinerPermissionRaw(
                block, permissionedMiningPolicy);
        }

        internal static Func<Block<NCAction>, BlockPolicyViolationException>
            ValidateTxCountPerBlockFactory(
                MinTransactionsPerBlockPolicy? minTransactionsPerBlockPolicy)
        {
            return block => ValidateTxCountPerBlockRaw(
                block,
                minTransactionsPerBlockPolicy);
        }

        internal static bool IsAllowedToMineRaw(
            BlockChain<NCAction> blockChain,
            Address miner,
            long index,
            Func<long, bool> isAuthorizedMiningBlockIndex,
            Func<Address, long, bool> isAuthorizedToMine,
            Func<long, bool> isPermissionedMiningBlockIndex,
            Func<Address, long, bool> isPermissionedToMine)
        {
            // For genesis blocks, any miner is allowed to mine.
            if (index == 0)
            {
                return true;
            }
            else if (isAuthorizedMiningBlockIndex(index))
            {
                return isAuthorizedToMine(miner, index);
            }
            else if (isPermissionedMiningBlockIndex(index))
            {
                return isPermissionedToMine(miner, index);
            }

            // If none of the conditions apply, any miner is allowed to mine.
            return true;
        }

        internal static Func<BlockChain<NCAction>, Address, long, bool> IsAllowedToMineFactory(
            Func<long, bool> isAuthorizedMiningBlockIndex,
            Func<Address, long, bool> isAuthorizedToMine,
            Func<long, bool> isPermissionedMiningBlockIndex,
            Func<Address, long, bool> isPermissionedToMine)
        {
            return (blockChain, miner, index) =>
                IsAllowedToMineRaw(
                    blockChain,
                    miner,
                    index,
                    isAuthorizedMiningBlockIndex,
                    isAuthorizedToMine,
                    isPermissionedMiningBlockIndex,
                    isPermissionedToMine);
        }

        /// <summary>
        /// Checks if authorized mining policy applies to given block index.
        /// </summary>
        /// <remarks>
        /// An implementation should be agnostic about other policies affecting the same index.
        /// Policy overruling between different policies should be handled elsewhere.
        /// </remarks>
        internal static bool IsAuthorizedMiningBlockIndexRaw(
            long index, AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            if (authorizedMiningPolicy is AuthorizedMiningPolicy amp)
            {
                return amp.IsTargetBlockIndex(index);
            }
            else
            {
                return false;
            }
        }

        internal static Func<long, bool> IsAuthorizedMiningBlockIndexFactory(
            AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            return index => IsAuthorizedMiningBlockIndexRaw(index, authorizedMiningPolicy);
        }


        /// <summary>
        /// Checks if given miner is allowed to mine a block with given index according to
        /// authorized mining policy.
        /// </summary>
        /// <remarks>
        /// An implementation should be agnostic about other policies affecting the same index.
        /// Policy overruling between different policies should be handled elsewhere.
        /// </remarks>
        internal static bool IsAuthorizedToMineRaw(
            Address miner, long index, AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            if (IsAuthorizedMiningBlockIndexRaw(index, authorizedMiningPolicy))
            {
                if (authorizedMiningPolicy is AuthorizedMiningPolicy amp)
                {
                    return amp.Miners.Contains(miner);
                }
                else
                {
                    throw new ArgumentException(
                        $"Result of {nameof(authorizedMiningPolicy)} cannot be null.");
                }
            }
            else
            {
                throw new ArgumentException(
                    $"Result of {nameof(authorizedMiningPolicy)} must be true.");
            }
        }

        internal static Func<Address, long, bool> IsAuthorizedToMineFactory(
            AuthorizedMiningPolicy? authorizedMiningPolicy)
        {
            return (miner, index) => IsAuthorizedToMineRaw(miner, index, authorizedMiningPolicy);
        }

        /// <summary>
        /// Checks if permissioned mining policy applies to given block index.
        /// </summary>
        /// <remarks>
        /// An implementation should be agnostic about other policies affecting the same index.
        /// Policy overruling between different policies should be handled elsewhere.
        /// </remarks>
        internal static bool IsPermissionedMiningBlockIndexRaw(
            long index,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            if (permissionedMiningPolicy is PermissionedMiningPolicy pmp)
            {
                return pmp.IsTargetBlockIndex(index);
            }
            else
            {
                return false;
            }
        }

        internal static Func<long, bool> IsPermissionedMiningBlockIndexFactory(
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            return index => IsPermissionedMiningBlockIndexRaw(index, permissionedMiningPolicy);
        }

        /// <summary>
        /// Checks if given miner is allowed to mine a block with given index according to
        /// permissioned mining policy.
        /// </summary>
        /// <remarks>
        /// An implementation should be agnostic about other policies affecting the same index.
        /// Policy overruling between different policies should be handled elsewhere.
        /// </remarks>
        internal static bool IsPermissionedToMineRaw(
            Address miner,
            long index,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            if (IsPermissionedMiningBlockIndexRaw(index, permissionedMiningPolicy))
            {
                if (permissionedMiningPolicy is PermissionedMiningPolicy pmp)
                {
                    return pmp.Miners.Contains(miner);
                }
                else
                {
                    throw new ArgumentException(
                        $"Argument {nameof(permissionedMiningPolicy)} cannot be null.");
                }
            }
            else
            {
                throw new ArgumentException(
                    $"Result of {nameof(IsPermissionedMiningBlockIndexRaw)} must be true.");
            }
        }

        internal static Func<Address, long, bool>
            IsPermissionedToMineFactory(PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            return (miner, index) => IsPermissionedToMineRaw(
                miner, index, permissionedMiningPolicy);
        }
    }
}
