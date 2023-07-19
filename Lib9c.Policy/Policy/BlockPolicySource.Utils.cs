using System;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Blockchain;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Blockchain.Policy
{
    // Collection of helper methods not directly used as a pluggable component.
    public partial class BlockPolicySource
    {
        /// <summary>
        /// <para>
        /// Checks if <paramref name="transaction"/> includes any <see cref="IAction"/> that is
        /// obsolete according to <see cref="ActionObsoleteAttribute"/> attached.
        /// </para>
        /// <para>
        /// Due to a bug, an <see cref="IAction"/> is considered obsolete starting from
        /// <see cref="ActionObsoleteAttribute.ObsoleteIndex"/> + 2.
        /// </para>
        /// </summary>
        /// <param name="transaction">The <see cref="Transaction{T}"/> to consider.</param>
        /// <param name="actionLoader">The loader to use <see cref="IAction"/>s included
        /// in <paramref name="transaction"/>.</param>
        /// <param name="blockIndex">Either the index of a prospective block to include
        /// <paramref name="transaction"/> or the index of a <see cref="Block{T}"/> containing
        /// <paramref name="transaction"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="transaction"/> includes any
        /// <see cref="IAction"/> that is obsolete, <see langword="false"/> otherwise.</returns>
        /// <seealso cref="ActionObsoleteAttribute"/>
        internal static bool IsObsolete(
            ITransaction transaction,
            IActionLoader actionLoader,
            long blockIndex
        )
        {
            if (!(transaction.Actions is { } rawActions))
            {
                return false;
            }
            else
            {
                try
                {
                    // Comparison with ObsoleteIndex + 2 is intended to have backward
                    // compatibility with a bugged original implementation.
                    return rawActions
                        .Select(rawAction => actionLoader.LoadAction(blockIndex, rawAction))
                        .Select(action => action.GetType())
                        .Any(actionType =>
                            actionType.GetCustomAttribute<ActionObsoleteAttribute>(false) is { } attribute &&
                            attribute.ObsoleteIndex + 2 <= blockIndex);
                }
                catch (Exception)
                {
                    // NOTE: Return false on fail to load
                    return true;
                }
            }
        }

        internal static bool IsAdminTransaction(BlockChain blockChain, Transaction transaction)
        {
            return GetAdminState(blockChain) is AdminState admin
                && admin.AdminAddress.Equals(transaction.Signer);
        }

        internal static AdminState GetAdminState(BlockChain blockChain)
        {
            return blockChain.GetState(AdminState.Address) is Dictionary rawAdmin
                ? new AdminState(rawAdmin)
                : null;
        }

        private static InvalidBlockBytesLengthException ValidateTransactionsBytesRaw(
            Block block,
            IVariableSubPolicy<long> maxTransactionsBytesPolicy)
        {
            long maxTransactionsBytes = maxTransactionsBytesPolicy.Getter(block.Index);
            long transactionsBytes = block.MarshalBlock().EncodingLength;

            if (transactionsBytes > maxTransactionsBytes)
            {
                return new InvalidBlockBytesLengthException(
                    $"The size of block #{block.Index} {block.Hash} is too large where " +
                    $"the maximum number of bytes allowed for transactions is " +
                    $"{maxTransactionsBytes}: {transactionsBytes}",
                    transactionsBytes);
            }

            return null;
        }

        private static BlockPolicyViolationException ValidateTxCountPerBlockRaw(
            Block block,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy)
        {
            int minTransactionsPerBlock =
                minTransactionsPerBlockPolicy.Getter(block.Index);
            int maxTransactionsPerBlock =
                maxTransactionsPerBlockPolicy.Getter(block.Index);

            if (block.Transactions.Count < minTransactionsPerBlock)
            {
                return new InvalidBlockTxCountException(
                    $"Block #{block.Index} {block.Hash} should include " +
                    $"at least {minTransactionsPerBlock} transaction(s): " +
                    $"{block.Transactions.Count}",
                    block.Transactions.Count);
            }
            else if (block.Transactions.Count > maxTransactionsPerBlock)
            {
                return new InvalidBlockTxCountException(
                    $"Block #{block.Index} {block.Hash} should include " +
                    $"at most {maxTransactionsPerBlock} transaction(s): " +
                    $"{block.Transactions.Count}",
                    block.Transactions.Count);
            }

            return null;
        }

        private static BlockPolicyViolationException ValidateTxCountPerSignerPerBlockRaw(
            Block block,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy)
        {
            int maxTransactionsPerSignerPerBlock =
                maxTransactionsPerSignerPerBlockPolicy.Getter(block.Index);
            var groups = block.Transactions
                .GroupBy(tx => tx.Signer)
                .Where(group => group.Count() > maxTransactionsPerSignerPerBlock);
            var offendingGroup = groups.FirstOrDefault();

            if (!(offendingGroup is null))
            {
                int offendingGroupCount = offendingGroup.Count();
                return new InvalidBlockTxCountPerSignerException(
                    $"Block #{block.Index} {block.Hash} includes too many " +
                    $"transactions from signer {offendingGroup.Key} where " +
                    $"the maximum number of transactions allowed by a single signer " +
                    $"per block is {maxTransactionsPerSignerPerBlock}: " +
                    $"{offendingGroupCount}",
                    offendingGroup.Key,
                    offendingGroupCount);
            }

            return null;
        }
    }
}
