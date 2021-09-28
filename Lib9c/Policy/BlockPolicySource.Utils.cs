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

        internal static bool IsAuthorizedMinerTransaction(
            BlockChain<NCAction> blockChain, Transaction<NCAction> transaction)
        {
            return GetAuthorizedMinersState(blockChain) is AuthorizedMinersState ams
                && ams.Miners.Contains(transaction.Signer);
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

        internal static AuthorizedMinersState GetAuthorizedMinersState(
            BlockChain<NCAction> blockChain)
        {
            try
            {
                return blockChain.GetState(AuthorizedMinersState.Address) is Dictionary rawAms
                    ? new AuthorizedMinersState(rawAms)
                    : null;
            }
            catch (IncompleteBlockStatesException)
            {
                return null;
            }
        }

        internal static BlockPolicyViolationException ValidateTxCountPerBlockRaw(
            Block<NCAction> block, bool ignoreHardcodedPolicies)
        {
            if (!(block.Miner is Address miner))
            {
                return null;
            }

            // To prevent selfish mining, we define a consensus that blocks with no transactions
            // are not accepted starting from MinTransactionsPerBlockHardcodedIndex.
            if (block.Transactions.Count < MinTransactionsPerBlock)
            {
                if (ignoreHardcodedPolicies)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} mined by {miner} should " +
                        "include at least one transaction. (Forced failure)");
                }
                else if (block.Index >= MinTransactionsPerBlockHardcodedIndex)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} mined by {miner} should " +
                        $"include at least {MinTransactionsPerBlock} transaction(s).");
                }
            }

            return null;
        }

        internal static BlockPolicyViolationException ValidateMinerPermissionRaw(
            Block<NCAction> block,
            PermissionedMiningPolicy? permissionedMiningPolicy,
            bool ignoreHardcodedPolicies)
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
                if (ignoreHardcodedPolicies)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} is not mined by a permissioned miner.  "
                            + "(Forced failure)");
                }
                else if (block.Index >= pmp.StartIndex)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash} is not mined by a permissioned miner.");
                }
            }

            return null;
        }

        internal static Func<Block<NCAction>, BlockPolicyViolationException>
            ValidateMinerPermissionFactory(
                PermissionedMiningPolicy? permissionedMiningPolicy,
                bool ignoreHardcodedPolicies)
        {
            return block => ValidateMinerPermissionRaw(
                block, permissionedMiningPolicy, ignoreHardcodedPolicies);
        }

        internal static Func<Block<NCAction>, BlockPolicyViolationException>
            ValidateTxCountPerBlockFactory(bool ignoreHardcodedPolicies)
        {
            return block => ValidateTxCountPerBlockRaw(block, ignoreHardcodedPolicies);
        }

        internal static BlockPolicyViolationException ValidateMinerAuthorityRaw(
            BlockChain<NCAction> blockChain, Block<NCAction> block, bool ignoreHardcodedPolicies)
        {
            Address miner = block.Miner;

            // FIXME: Uninstantiated blockChain can be passed as an argument.
            // Until this is fixed, it is crucial block index is checked first.
            // Authorized minor validity is only checked for certain indices.
            if (block.Index == 0)
            {
                return null;
            }
            if (!IsAuthorizedMiningBlockIndex(blockChain, block.Index))
            {
                return null;
            }
            // If AuthorizedMinorState is null, do not check miner authority.
            else if (!(GetAuthorizedMinersState(blockChain) is AuthorizedMinersState ams))
            {
                return null;
            }
            // Otherwise, block's miner should be one of the authorized miners.
            else if (!ams.Miners.Contains(miner))
            {
                return new BlockPolicyViolationException(
                    $"The block #{block.Index} {block.Hash} is not mined by an authorized miner.");
            }

            // Authority is proven through a no-op transaction, i.e. a transaction
            // with zero actions, starting from ValidateMinerAuthorityNoOpHardcodedIndex.
            Transaction<NCAction>[] txs = block.Transactions.ToArray();
            if (!txs.Any(tx => tx.Signer.Equals(miner) && !tx.Actions.Any())
                    && block.ProtocolVersion > 0)
            {
                if (ignoreHardcodedPolicies)
                {
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash}'s miner {miner} should be proven by "
                            + "including a no-op transaction by signed the same authority.  "
                            + "(Forced failure)");
                }
                else if (block.Index >= ValidateMinerAuthorityNoOpTxHardcodedIndex)
                {
#if DEBUG
                    string debug =
                        "  Note that there " +
                        (txs.Length == 1 ? "is a transaction:" : $"are {txs.Length} transactions:") +
                        txs.Select((tx, i) =>
                                $"\n    {i}. {tx.Actions.Count} actions; signed by {tx.Signer}")
                            .Aggregate(string.Empty, (a, b) => a + b);
#else
                    const string debug = "";
#endif
                    return new BlockPolicyViolationException(
                        $"Block #{block.Index} {block.Hash}'s miner {miner} should be proven by " +
                        "including a no-op transaction by signed the same authority." + debug);
                }
            }

            return null;
        }

        internal static Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException>
            ValidateMinerAuthorityFactory(bool ignoreHardcodedPolicies)
        {
            return (blockChain, block) =>
                ValidateMinerAuthorityRaw(blockChain, block, ignoreHardcodedPolicies);
        }

        internal static bool IsAllowedToMineRaw(
            BlockChain<NCAction> blockChain,
            Address miner,
            long index,
            Func<BlockChain<NCAction>, long, bool> isPermissionedMiningBlockIndex,
            Func<BlockChain<NCAction>, Address, long, bool> isPermissionedToMine)
        {
            // For genesis blocks, any miner is allowed to mine.
            if (index == 0)
            {
                return true;
            }
            else if (IsAuthorizedMiningBlockIndex(blockChain, index))
            {
                return IsAuthorizedToMine(blockChain, miner, index);
            }
            else if (isPermissionedMiningBlockIndex(blockChain, index))
            {
                return isPermissionedToMine(blockChain, miner, index);
            }

            // If none of the conditions apply, any miner is allowed to mine.
            return true;
        }

        internal static Func<BlockChain<NCAction>, Address, long, bool> IsAllowedToMineFactory(
            Func<BlockChain<NCAction>, long, bool> isPermissionedMiningBlockIndex,
            Func<BlockChain<NCAction>, Address, long, bool> isPermissionedToMine)
        {
            return (blockChain, miner, index) =>
                IsAllowedToMineRaw(
                    blockChain, miner, index, isPermissionedMiningBlockIndex, isPermissionedToMine);
        }

        /// <summary>
        /// Checks if authorized mining policy applies to given block index.
        /// </summary>
        /// <remarks>
        /// An implementation should be agnostic about other policies affecting the same index.
        /// Policy overruling between different policies should be handled elsewhere.
        /// </remarks>
        internal static bool IsAuthorizedMiningBlockIndex(
            BlockChain<NCAction> blockChain, long index)
        {
            if (GetAuthorizedMinersState(blockChain) is AuthorizedMinersState ams)
            {
                return index % ams.Interval == 0 && index <= ams.ValidUntil;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if given miner is allowed to mine a block with given index according to
        /// authorized mining policy.
        /// </summary>
        /// <remarks>
        /// An implementation should be agnostic about other policies affecting the same index.
        /// Policy overruling between different policies should be handled elsewhere.
        /// </remarks>
        internal static bool IsAuthorizedToMine(
            BlockChain<NCAction> blockChain, Address miner, long index)
        {
            if (IsAuthorizedMiningBlockIndex(blockChain, index))
            {
                if (GetAuthorizedMinersState(blockChain) is AuthorizedMinersState ams)
                {
                    return ams.Miners.Contains(miner);
                }
                else
                {
                    throw new ArgumentException(
                        $"Result of {nameof(GetAuthorizedMinersState)} cannot be null.");
                }
            }
            else
            {
                throw new ArgumentException(
                    $"Result of {nameof(IsAuthorizedMiningBlockIndex)} must be true.");
            }
        }

        /// <summary>
        /// Checks if permissioned mining policy applies to given block index.
        /// </summary>
        /// <remarks>
        /// An implementation should be agnostic about other policies affecting the same index.
        /// Policy overruling between different policies should be handled elsewhere.
        /// </remarks>
        internal static bool IsPermissionedMiningBlockIndexRaw(
            BlockChain<NCAction> blockChain,
            long index,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            if (permissionedMiningPolicy is PermissionedMiningPolicy pmp)
            {
                return index >= pmp.StartIndex;
            }
            else
            {
                return false;
            }
        }

        internal static Func<BlockChain<NCAction>, long, bool>
            IsPermissionedMiningBlockIndexFactory(
                PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            return (blockChain, index) => IsPermissionedMiningBlockIndexRaw(
                blockChain, index, permissionedMiningPolicy);
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
            BlockChain<NCAction> blockChain,
            Address miner,
            long index,
            PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            if (IsPermissionedMiningBlockIndexRaw(blockChain, index, permissionedMiningPolicy))
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

        internal static Func<BlockChain<NCAction>, Address, long, bool>
            IsPermissionedToMineFactory(PermissionedMiningPolicy? permissionedMiningPolicy)
        {
            return (blockChain, miner, index) => IsPermissionedToMineRaw(
                blockChain, miner, index, permissionedMiningPolicy);
        }
    }
}
