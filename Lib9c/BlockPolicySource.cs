using System;
using System.Collections.Generic;
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
using Libplanet.Blockchain.Renderers;
using Serilog;
using Serilog.Events;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.BlockChain
{
    public class BlockPolicySource
    {
        private readonly TimeSpan _blockInterval = TimeSpan.FromSeconds(8);

        public readonly ActionRenderer ActionRenderer = new ActionRenderer();

        public readonly BlockRenderer BlockRenderer = new BlockRenderer();

        public readonly LoggedActionRenderer<NCAction> LoggedActionRenderer;

        public readonly LoggedRenderer<NCAction> LoggedBlockRenderer;

        public BlockPolicySource(ILogger logger, LogEventLevel logEventLevel = LogEventLevel.Verbose)
        {
            BlockRenderer
                .EveryBlock()
                .Subscribe(_ => UpdateActivationSet());

            BlockRenderer
                .EveryReorg()
                .Subscribe(_ => UpdateActivationSet());

            LoggedActionRenderer =
                new LoggedActionRenderer<NCAction>(ActionRenderer, logger, logEventLevel);

            LoggedBlockRenderer =
                new LoggedRenderer<NCAction>(BlockRenderer, logger, logEventLevel);
        }

        public Func<IValue> ActivatedAccountsStateGetter { get; set; }

        public IImmutableSet<Address> ActivatedAccounts { get; private set; }

        public AuthorizedMinersState AuthorizedMinersState { get; set; }

        public void UpdateActivationSet(IValue state)
        {
            ActivatedAccounts = new ActivatedAccountsState((Dictionary)state).Accounts;
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public IBlockPolicy<NCAction> GetPolicy(int minimumDifficulty)
        {
            ActivatedAccounts = ActivatedAccounts?.Clear();
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            return new BlockPolicy(
                new BlockPolicy<NCAction>(
                    new RewardGold(),
                    _blockInterval,
                    minimumDifficulty,
                    2048,
                    doesTransactionFollowPolicy: IsSignerAuthorized
                ),
                ValidateBlock
            );
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

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

        private void UpdateActivationSet()
        {
            if (!(ActivatedAccountsStateGetter is null))
            {
                IValue state = ActivatedAccountsStateGetter();
                UpdateActivationSet(state);
            }
        }

        private InvalidBlockException ValidateBlock(Block<NCAction> block)
        {
            if (AuthorizedMinersState is null)
            {
                return null;
            }

            if (!(block.Miner is Address miner))
            {
                return null;
            }

            bool targetBlock =
                0 < block.Index && block.Index <= AuthorizedMinersState.ValidUntil &&
                block.Index % AuthorizedMinersState.Interval == 0;
            bool minedByAuthorities = AuthorizedMinersState.Miners.Contains(miner);
            
            if (targetBlock && !minedByAuthorities)
            {
                return new InvalidMinerException(
                    $"The given block[{block}] isn't mined by authorities.",
                    miner
                );
            }

            return null;
        }
    }
}
