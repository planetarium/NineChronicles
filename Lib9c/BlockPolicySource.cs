using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
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
    public class BlockPolicySource
    {
        private readonly TimeSpan _blockInterval = TimeSpan.FromSeconds(8);

        private readonly ActionRenderer _actionRenderer = new ActionRenderer();

        public BlockPolicySource()
        {
            _actionRenderer
                .EveryRender(ActivatedAccountsState.Address)
                .Subscribe(UpdateActivationSet);

            _actionRenderer
                .EveryUnrender(ActivatedAccountsState.Address)
                .Subscribe(UpdateActivationSet);
        }

        public IImmutableSet<Address> ActivatedAccounts { get; private set; }

        public void UpdateActivationSet(IValue state)
        {
            ActivatedAccounts = new ActivatedAccountsState((Dictionary)state).Accounts;
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public IBlockPolicy<PolymorphicAction<ActionBase>> GetPolicy(int minimumDifficulty)
        {
            ActivatedAccounts = ActivatedAccounts?.Clear();
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            return new BlockPolicy<PolymorphicAction<ActionBase>>(
                new RewardGold(),
                _blockInterval,
                minimumDifficulty,
                2048,
                doesTransactionFollowPolicy: IsSignerAuthorized
            );
#endif
        }

        public ActionRenderer GetRenderer() => _actionRenderer;

        private bool IsSignerAuthorized(Transaction<PolymorphicAction<ActionBase>> transaction)
        {
            bool isActivateAccountAction =
                transaction.Actions.Count == 1
                && transaction.Actions.First().InnerAction is ActivateAccount;

            return isActivateAccountAction
                   || ActivatedAccounts is null
                   || !ActivatedAccounts.Any()
                   || ActivatedAccounts.Contains(transaction.Signer);
        }

        private void UpdateActivationSet(ActionBase.ActionEvaluation<ActionBase> evaluation)
        {
            UpdateActivationSet(evaluation.OutputStates.GetState(ActivatedAccountsState.Address));
        }
    }
}
