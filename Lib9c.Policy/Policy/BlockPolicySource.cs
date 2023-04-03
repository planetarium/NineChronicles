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
        public const int MaxTransactionsPerBlock = 100;

        public static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

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
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-internal deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetInternalPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Internal,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Internal);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-permanent-test deployment.
        /// </summary>
        public IBlockPolicy<NCAction> GetPermanentPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance identical to the one deployed
        /// except with lower minimum difficulty for faster testing and benchmarking.
        /// </summary>
        public IBlockPolicy<NCAction> GetTestPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for networks
        /// with default options, without authorized mining and permissioned mining.
        /// </summary>
        public IBlockPolicy<NCAction> GetDefaultPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Default,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Default,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Default,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Default);

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
        /// <returns>A <see cref="BlockPolicy"/> constructed from given parameters.</returns>
        internal IBlockPolicy<NCAction> GetPolicy(
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy)
        {
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");
             return new DebugPolicy();
#else
            maxTransactionsBytesPolicy = maxTransactionsBytesPolicy
                ?? MaxTransactionsBytesPolicy.Default;
            minTransactionsPerBlockPolicy = minTransactionsPerBlockPolicy
                ?? MinTransactionsPerBlockPolicy.Default;
            maxTransactionsPerBlockPolicy = maxTransactionsPerBlockPolicy
                ?? MaxTransactionsPerBlockPolicy.Default;
            maxTransactionsPerSignerPerBlockPolicy = maxTransactionsPerSignerPerBlockPolicy
                ?? MaxTransactionsPerSignerPerBlockPolicy.Default;

            Func<BlockChain<NCAction>, Transaction<NCAction>, TxPolicyViolationException> validateNextBlockTx =
                (blockChain, transaction) => ValidateNextBlockTxRaw(
                    blockChain, _actionTypeLoader, transaction);
            Func<BlockChain<NCAction>, Block<NCAction>, BlockPolicyViolationException> validateNextBlock =
                (blockchain, block) => ValidateNextBlockRaw(
                    block,
                    maxTransactionsBytesPolicy,
                    minTransactionsPerBlockPolicy,
                    maxTransactionsPerBlockPolicy,
                    maxTransactionsPerSignerPerBlockPolicy);

            // FIXME: Slight inconsistency due to pre-existing delegate.
            return new BlockPolicy(
                new RewardGold(),
                blockInterval: BlockInterval,
                validateNextBlockTx: validateNextBlockTx,
                validateNextBlock: validateNextBlock,
                getMaxTransactionsBytes: maxTransactionsBytesPolicy.Getter,
                getMinTransactionsPerBlock: minTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerBlock: maxTransactionsPerBlockPolicy.Getter,
                getMaxTransactionsPerSignerPerBlock: maxTransactionsPerSignerPerBlockPolicy.Getter);
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        internal static TxPolicyViolationException ValidateNextBlockTxRaw(
            BlockChain<NCAction> blockChain,
            IActionTypeLoader actionTypeLoader,
            Transaction<NCAction> transaction)
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
            Block<NCAction> nextBlock,
            IVariableSubPolicy<long> maxTransactionsBytesPolicy,
            IVariableSubPolicy<int> minTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerBlockPolicy,
            IVariableSubPolicy<int> maxTransactionsPerSignerPerBlockPolicy)
        {
            if (ValidateTransactionsBytesRaw(
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
            }

            return null;
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
