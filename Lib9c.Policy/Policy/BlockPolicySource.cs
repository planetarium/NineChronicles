using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blockchain.Renderers;
using Nekoyume.Action;
using Nekoyume.Action.Loader;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Serilog;
using Serilog.Events;
using Lib9c;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using Libplanet.Types.Tx;

#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
#endif

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Model;
#endif

namespace Nekoyume.Blockchain.Policy
{
    public partial class BlockPolicySource
    {
        public const int MaxTransactionsPerBlock = 100;

        public static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        private readonly IActionLoader _actionLoader;

        // FIXME: Why does BlockPolicySource have renderers?
        public readonly ActionRenderer ActionRenderer = new ActionRenderer();

        // FIXME: Why does BlockPolicySource have renderers?
        public readonly BlockRenderer BlockRenderer = new BlockRenderer();

        // FIXME: Why does BlockPolicySource have renderers?
        public readonly LoggedActionRenderer LoggedActionRenderer;

        // FIXME: Why does BlockPolicySource have renderers?
        public readonly LoggedRenderer LoggedBlockRenderer;

        public BlockPolicySource(
            ILogger logger,
            LogEventLevel logEventLevel = LogEventLevel.Verbose,
            IActionLoader actionLoader = null)
        {
            _actionLoader ??= new NCActionLoader();

            LoggedActionRenderer =
                new LoggedActionRenderer(ActionRenderer, logger, logEventLevel);

            LoggedBlockRenderer =
                new LoggedRenderer(BlockRenderer, logger, logEventLevel);
        }

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-main deployment.
        /// </summary>
        public IBlockPolicy GetPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-internal deployment.
        /// </summary>
        public IBlockPolicy GetInternalPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Internal,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Internal);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for 9c-permanent-test deployment.
        /// </summary>
        public IBlockPolicy GetPermanentPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance identical to the one deployed
        /// except with lower minimum difficulty for faster testing and benchmarking.
        /// </summary>
        public IBlockPolicy GetTestPolicy() =>
            GetPolicy(
                maxTransactionsBytesPolicy: MaxTransactionsBytesPolicy.Mainnet,
                minTransactionsPerBlockPolicy: MinTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerBlockPolicy: MaxTransactionsPerBlockPolicy.Mainnet,
                maxTransactionsPerSignerPerBlockPolicy: MaxTransactionsPerSignerPerBlockPolicy.Mainnet);

        /// <summary>
        /// Creates an <see cref="IBlockPolicy{T}"/> instance for networks
        /// with default options, without authorized mining and permissioned mining.
        /// </summary>
        public IBlockPolicy GetDefaultPolicy() =>
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
        internal IBlockPolicy GetPolicy(
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

            Func<BlockChain, Transaction, TxPolicyViolationException> validateNextBlockTx =
                (blockChain, transaction) => ValidateNextBlockTxRaw(
                    blockChain, _actionLoader, transaction);
            Func<BlockChain, Block, BlockPolicyViolationException> validateNextBlock =
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

        public IEnumerable<IRenderer> GetRenderers() =>
            new IRenderer[] { BlockRenderer, LoggedActionRenderer };

        internal static TxPolicyViolationException ValidateNextBlockTxRaw(
            BlockChain blockChain,
            IActionLoader actionLoader,
            Transaction transaction)
        {
            // Avoid NRE when genesis block appended
            long index = blockChain.Count > 0 ? blockChain.Tip.Index + 1: 0;

            if (((ITransaction)transaction).Actions?.Count > 1)
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} has too many actions: " +
                    $"{((ITransaction)transaction).Actions?.Count}",
                    transaction.Id);
            }
            else if (IsObsolete(transaction, actionLoader, index))
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} is obsolete.",
                    transaction.Id);
            }

            try
            {
                if (blockChain.GetBalance(MeadConfig.PatronAddress, Currencies.Mead) < 1 * Currencies.Mead)
                {
                    // Check Activation
                    try
                    {
                        if (transaction.Actions is { } rawActions &&
                            rawActions.Count == 1 &&
                            actionLoader.LoadAction(index, rawActions.First()) is ActionBase action &&
                            action is IActivateAccount activate)
                        {
                            return transaction.Nonce == 0 &&
                                blockChain.GetState(activate.PendingAddress) is Dictionary rawPending &&
                                new PendingActivationState(rawPending).Verify(activate.Signature)
                                    ? null
                                    : new TxPolicyViolationException(
                                        $"Transaction {transaction.Id} has an invalid activate action.",
                                        transaction.Id);
                        }
                    }
                    catch (Exception e)
                    {
                        return new TxPolicyViolationException(
                            $"Transaction {transaction.Id} has an invalid action.",
                            transaction.Id,
                            e);
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
                }
                if (transaction.MaxGasPrice is null || transaction.GasLimit is null)
                {
                    return new
                        TxPolicyViolationException("Transaction has no gas price or limit.",
                        transaction.Id);
                }
                if (transaction.MaxGasPrice * transaction.GasLimit > blockChain.GetBalance(transaction.Signer, Currencies.Mead))
                {
                    return new TxPolicyViolationException(
                        $"Transaction {transaction.Id} signer insufficient transaction fee",
                        transaction.Id);
                }
            }
            catch (InvalidSignatureException)
            {
                return new TxPolicyViolationException(
                    $"Transaction {transaction.Id} has invalid signautre.",
                    transaction.Id);
            }

            return null;
        }

        internal static BlockPolicyViolationException ValidateNextBlockRaw(
            Block nextBlock,
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
