using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Lib9c.Renderer;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
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
            LoggedActionRenderer =
                new LoggedActionRenderer<NCAction>(ActionRenderer, logger, logEventLevel);

            LoggedBlockRenderer =
                new LoggedRenderer<NCAction>(BlockRenderer, logger, logEventLevel);
        }

        // FIXME 남은 설정들도 설정화 해야 할지도?
        public IBlockPolicy<NCAction> GetPolicy(int minimumDifficulty)
        {
#if UNITY_EDITOR
            return new DebugPolicy();
#else
            return new BlockPolicy(
                new RewardGold(),
                _blockInterval,
                minimumDifficulty,
                2048,
                IsSignerAuthorized
            );
#endif
        }

        public IEnumerable<IRenderer<NCAction>> GetRenderers() =>
            new IRenderer<NCAction>[] { BlockRenderer, LoggedActionRenderer };

        private bool IsSignerAuthorized(
            Transaction<NCAction> transaction,
            BlockChain<NCAction> blockChain
        )
        {
            try
            {
                if (transaction.Actions.Count == 1 &&
                    transaction.Actions.First().InnerAction is ActivateAccount aa)
                {
                    return blockChain.GetState(aa.PendingAddress) is Dictionary rawPending &&
                        new PendingActivationState(rawPending).Verify(aa);
                }

                if (blockChain.GetState(ActivatedAccountsState.Address) is Dictionary asDict)
                {
                    IImmutableSet<Address> activatedAccounts =
                        new ActivatedAccountsState(asDict).Accounts;
                    return !activatedAccounts.Any() ||
                        activatedAccounts.Contains(transaction.Signer);
                }
                else
                {
                    return true;
                }
            }
            catch (InvalidSignatureException)
            {
                return false;
            }
            catch (IncompleteBlockStatesException)
            {
                // It can be caused during `Swarm<T>.PreloadAsync()` because it doesn't fill its 
                // state right away...
                return true;
            }
        }
    }
}
