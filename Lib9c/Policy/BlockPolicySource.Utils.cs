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

        internal static Func<Block<NCAction>, BlockPolicyViolationException>
            ValidateTxCountPerBlockFactory(bool ignoreHardcodedPolicies)
        {
            return block => ValidateTxCountPerBlockRaw(block, ignoreHardcodedPolicies);
        }
    }
}
