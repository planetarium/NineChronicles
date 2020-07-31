using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Libplanet;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Nekoyume.BlockChain
{
    public static class BlockPolicy
    {
        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(8);

        private static readonly ActionRenderer ActionRenderer = new ActionRenderer(
            ActionBase.RenderSubject,
            ActionBase.UnrenderSubject
        );

        static BlockPolicy()
        {
            ActionRenderer
                .EveryRender(ActivatedAccountsState.Address)
                .Subscribe(UpdateActivationSet);

            ActionRenderer
                .EveryUnrender(ActivatedAccountsState.Address)
                .Subscribe(UpdateActivationSet);
        }

        public static IImmutableSet<Address> ActivatedAccounts { get; private set; }

        public static void UpdateActivationSet(IValue state)
        {
            ActivatedAccounts = new ActivatedAccountsState((Dictionary)state).Accounts;
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public static IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy(int minimumDifficulty)
        {
            ActivatedAccounts = ActivatedAccounts?.Clear();
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                new RewardGold { Gold = 10 },
                BlockInterval,
                minimumDifficulty,
                2048,
                doesTransactionFollowPolicy: IsSignerAuthorized
            );
#endif
        }

        private static bool IsSignerAuthorized(Transaction<PolymorphicAction<ActionBase>> transaction)
        {
            bool isActivateAccountAction =
                transaction.Actions.Count == 1
                && transaction.Actions.First().InnerAction is ActivateAccount;

            return isActivateAccountAction
                   || ActivatedAccounts is null
                   || !ActivatedAccounts.Any()
                   || ActivatedAccounts.Contains(transaction.Signer);
        }

        private static void UpdateActivationSet(ActionBase.ActionEvaluation<ActionBase> evaluation)
        {
            UpdateActivationSet(evaluation.OutputStates.GetState(ActivatedAccountsState.Address));
        }

        private class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
        {
            public IAction BlockAction { get; } = new RewardGold { Gold = 10 };

            public InvalidBlockException ValidateNextBlock(
                BlockChain<PolymorphicAction<ActionBase>> blocks,
                Block<PolymorphicAction<ActionBase>> nextBlock
            )
            {
                return null;
            }

            public long GetNextBlockDifficulty(BlockChain<PolymorphicAction<ActionBase>> blocks)
            {
                return blocks.Tip is null ? 0 : 1;
            }

            public bool DoesTransactionFollowsPolicy(
                Transaction<PolymorphicAction<ActionBase>> transaction
            ) =>
                true;
        }
    }
}
