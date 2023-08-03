namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Xunit;

    public class MeadScenarioTest
    {
        [Fact]
        public void Contract()
        {
            Currency mead = Currencies.Mead;
            var patron = new PrivateKey().ToAddress();
            IActionContext context = new ActionContext();
            IAccountStateDelta states = new MockStateDelta().MintAsset(context, patron, 10 * mead);

            var agentAddress = new PrivateKey().ToAddress();
            var requestPledge = new RequestPledge
            {
                AgentAddress = agentAddress,
                RefillMead = RequestPledge.DefaultRefillMead,
            };
            var states2 = Execute(context, states, requestPledge, patron);
            Assert.Equal(8 * mead, states2.GetBalance(patron, mead));
            Assert.Equal(1 * mead, states2.GetBalance(agentAddress, mead));

            var approvePledge = new ApprovePledge
            {
                PatronAddress = patron,
            };
            var states3 = Execute(context, states2, approvePledge, agentAddress);
            Assert.Equal(4 * mead, states3.GetBalance(patron, mead));
            Assert.Equal(4 * mead, states3.GetBalance(agentAddress, mead));

            // release and return agent mead
            var endPledge = new EndPledge
            {
                AgentAddress = agentAddress,
            };
            var states4 = Execute(context, states3, endPledge, patron);
            Assert.Equal(7 * mead, states4.GetBalance(patron, mead));
            Assert.Equal(0 * mead, states4.GetBalance(agentAddress, mead));

            // re-contract with Bencodex.Null
            var states5 = Execute(context, states4, requestPledge, patron);
            Assert.Equal(5 * mead, states5.GetBalance(patron, mead));
            Assert.Equal(1 * mead, states5.GetBalance(agentAddress, mead));

            var states6 = Execute(context, states5, approvePledge, agentAddress);
            Assert.Equal(1 * mead, states6.GetBalance(patron, mead));
            Assert.Equal(4 * mead, states6.GetBalance(agentAddress, mead));
        }

        [Fact]
        public void UseGas()
        {
            Type baseType = typeof(Nekoyume.Action.ActionBase);

            bool IsTarget(Type type)
            {
                return baseType.IsAssignableFrom(type) &&
                    type != typeof(InitializeStates) &&
                    type.GetCustomAttribute<ActionTypeAttribute>() is { } &&
                    (
                        !(type.GetCustomAttribute<ActionObsoleteAttribute>()?.ObsoleteIndex is { } obsoleteIndex) ||
                        obsoleteIndex > ActionObsoleteConfig.V200030ObsoleteIndex
                    );
            }

            var assembly = baseType.Assembly;
            var typeIds = assembly.GetTypes()
                .Where(IsTarget);
            long expectedTransferActionGasLimit = 4L;
            long expectedActionGasLimit = 1L;
            foreach (var typeId in typeIds)
            {
                var action = (IAction)Activator.CreateInstance(typeId)!;
                var actionContext = new ActionContext
                {
                    PreviousState = new MockStateDelta(),
                };
                try
                {
                    action.Execute(actionContext);
                }
                catch (Exception)
                {
                    // ignored
                }

                long expectedGasLimit = action is ITransferAsset || action is ITransferAssets
                    ? expectedTransferActionGasLimit
                    : expectedActionGasLimit;
                long gasUsed = actionContext.GasUsed();
                Assert.True(expectedGasLimit == gasUsed, $"{action} invalid used gas. {gasUsed}");
            }
        }

        private IAccountStateDelta Execute(IActionContext context, IAccountStateDelta state, IAction action, Address signer)
        {
            Assert.True(state.GetBalance(signer, Currencies.Mead) > 0 * Currencies.Mead);
            var nextState = state.BurnAsset(context, signer, 1 * Currencies.Mead);
            var executedState = action.Execute(new ActionContext
            {
                Signer = signer,
                PreviousState = nextState,
            });
            return RewardGold.TransferMead(context, executedState);
        }
    }
}
