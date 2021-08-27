using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using Lib9c.Renderer;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Libplanet;
using Libplanet.Blockchain.Renderers;
using Nekoyume.Model;
using Serilog;
using Serilog.Events;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
#endif
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain
{
    public class BlockPolicySource
    {
        public const int DifficultyBoundDivisor = 2048;

        // Note: The heaviest block of 9c-main (except for the genesis) weighs 58,408 B (58 KiB).
        public const int MaxBlockBytes = 1024 * 100; // 100 KiB

        // Note: The genesis block of 9c-main net weighs 11,085,640 B (11 MiB).
        public const int MaxGenesisBytes = 1024 * 1024 * 15; // 15 MiB

        public const long V100066ObsoleteIndex = 2200000;
        
        public const long V100068ObsoleteIndex = 2220000;

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
                permissionedMiningPolicy: PermissionedMiningPolicy.Mainnet
            );

        // FIXME 남은 설정들도 설정화 해야 할지도?
        internal IBlockPolicy<NCAction> GetPolicy(
            int minimumDifficulty,
            int maximumTransactions,
            PermissionedMiningPolicy? permissionedMiningPolicy,
            bool ignoreHardcodedPolicies
        )
        {
#if UNITY_EDITOR
            return new Lib9c.DebugPolicy();
#else
            return new BlockPolicy(
                new RewardGold(),
                blockInterval: _blockInterval,
                minimumDifficulty: minimumDifficulty,
                difficultyBoundDivisor: DifficultyBoundDivisor,
                maxTransactionsPerBlock: maximumTransactions,
                maxBlockBytes: MaxBlockBytes,
                maxGenesisBytes: MaxGenesisBytes,
                ignoreHardcodedPolicies: ignoreHardcodedPolicies,
                permissionedMiningPolicy: permissionedMiningPolicy,
                doesTransactionFollowPolicy: DoesTransactionFollowPolicy
            );
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

        private bool DoesTransactionFollowPolicy(
            Transaction<NCAction> transaction,
            BlockChain<NCAction> blockChain
        )
        {
            return CheckTransaction(transaction, blockChain);
        }

        private bool CheckTransaction(
            Transaction<NCAction> transaction,
            BlockChain<NCAction> blockChain
        )
        {
            // Avoid NRE when genesis block appended
            long index = blockChain.Count > 0 ? blockChain.Tip.Index : 0;
            if (transaction.Actions.Count > 1 || IsObsolete(transaction, index))
            {
                return false;
            }

            try
            {
                // Check if it is a no-op transaction to prove it's made by the authorized miner.
                if (blockChain.GetState(AuthorizedMinersState.Address) is Dictionary rawAms &&
                    new AuthorizedMinersState(rawAms).Miners.Contains(transaction.Signer))
                {
                    // The authorization proof has to have no actions at all.
                    return !transaction.Actions.Any();
                }

                // Check ActivateAccount
                if (transaction.Actions.Count == 1 &&
                    transaction.Actions.First().InnerAction is IActivateAction aa)
                {
                    return blockChain.GetState(aa.GetPendingAddress()) is Dictionary rawPending &&
                           new PendingActivationState(rawPending).Verify(aa.GetSignature());
                }

                // Check admin
                if (blockChain.GetState(Addresses.Admin) is Dictionary rawAdmin
                    && new AdminState(rawAdmin).AdminAddress.Equals(transaction.Signer))
                {
                    return true;
                }

                switch (blockChain.GetState(transaction.Signer.Derive(ActivationKey.DeriveKey)))
                {
                    case null:
                        // Fallback for pre-migration.
                        if (blockChain.GetState(ActivatedAccountsState.Address) is Dictionary asDict)
                        {
                            IImmutableSet<Address> activatedAccounts =
                                new ActivatedAccountsState(asDict).Accounts;
                            return !activatedAccounts.Any() ||
                                   activatedAccounts.Contains(transaction.Signer);
                        }
                        return true;
                    case Bencodex.Types.Boolean _:
                        return true;
                }

                return true;
            }
            catch (InvalidSignatureException)
            {
                return false;
            }
            catch (IncompleteBlockStatesException)
            {
                // It can be caused during `Swarm<T>.PreloadAsync()` because it doesn't fill its
                // state right away...
                // FIXME It should be removed after fix that Libplanet fills its state on IBD.
                // See also: https://github.com/planetarium/lib9c/pull/151#discussion_r506039478
                return true;
            }
        }
    }
}
