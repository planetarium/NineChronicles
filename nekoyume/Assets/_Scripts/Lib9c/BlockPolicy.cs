using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;
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
        private static readonly TimeSpan BlockInterval = TimeSpan.FromSeconds(10);

        private static readonly ActionRenderer ActionRenderer = new ActionRenderer(
            ActionBase.RenderSubject,
            ActionBase.UnrenderSubject
        );

        static BlockPolicy()
        {
            ActionRenderer
                .EveryRender(TableSheetsState.Address)
                .Subscribe(UpdateActivationSet);

            ActionRenderer
                .EveryUnrender(TableSheetsState.Address)
                .Subscribe(UpdateActivationSet);
        }

        public static ImmutableHashSet<PublicKey> ActivationSet { get; private set; }

        public static void UpdateActivationSet(IValue state)
        {
            var activationSheet = GetActivationSheet(state);

            ActivationSet = activationSheet?.Values
                .Select(row => row.PublicKey)
                .ToImmutableHashSet();
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public static IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy(int minimumDifficulty)
        {
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                new RewardGold { Gold = 1 },
                BlockInterval,
                minimumDifficulty,
                2048,
                doesTransactionFollowPolicy: IsSignerAuthorized
            );
#endif
        }

        private static ActivationSheet GetActivationSheet(IValue state)
        {
            if (state is null)
            {
                return null;
            }

            var tableSheetsState = new TableSheetsState((Dictionary)state);
            return TableSheets.FromTableSheetsState(tableSheetsState).ActivationSheet;
        }

        private static bool IsSignerAuthorized(Transaction<PolymorphicAction<ActionBase>> transaction)
        {
            var signerPublicKey = transaction.PublicKey;

            return ActivationSet is null
                   || ActivationSet.Count == 0
                   || ActivationSet.Contains(signerPublicKey);
        }

        private static void UpdateActivationSet(ActionBase.ActionEvaluation<ActionBase> evaluation)
        {
            var state = evaluation.OutputStates.GetState(TableSheetsState.Address);
            UpdateActivationSet(state);
        }

        private class DebugPolicy : IBlockPolicy<PolymorphicAction<ActionBase>>
        {
            public IAction BlockAction { get; } = new RewardGold {Gold = 1};

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
