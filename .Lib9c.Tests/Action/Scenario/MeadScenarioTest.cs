namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Action.Loader;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Libplanet.State;
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
            IAccountStateDelta states = new State().MintAsset(patron, 10 * mead);

            var agentAddress = new PrivateKey().ToAddress();
            var requestPledge = new RequestPledge
            {
                AgentAddress = agentAddress,
                Mead = RequestPledge.RefillMead,
            };
            var states2 = Execute(states, requestPledge, patron);
            Assert.Equal(8 * mead, states2.GetBalance(patron, mead));
            Assert.Equal(1 * mead, states2.GetBalance(agentAddress, mead));

            var approvePledge = new ApprovePledge
            {
                PatronAddress = patron,
            };
            var states3 = Execute(states2, approvePledge, agentAddress);
            Assert.Equal(4 * mead, states3.GetBalance(patron, mead));
            Assert.Equal(4 * mead, states3.GetBalance(agentAddress, mead));

            // release and return agent mead
            var endPledge = new EndPledge
            {
                AgentAddress = agentAddress,
            };
            var states4 = Execute(states3, endPledge, patron);
            Assert.Equal(7 * mead, states4.GetBalance(patron, mead));
            Assert.Equal(0 * mead, states4.GetBalance(agentAddress, mead));

            // re-contract with Bencodex.Null
            var states5 = Execute(states4, requestPledge, patron);
            Assert.Equal(5 * mead, states5.GetBalance(patron, mead));
            Assert.Equal(1 * mead, states5.GetBalance(agentAddress, mead));

            var states6 = Execute(states5, approvePledge, agentAddress);
            Assert.Equal(1 * mead, states6.GetBalance(patron, mead));
            Assert.Equal(4 * mead, states6.GetBalance(agentAddress, mead));
        }

        [Fact]
        public void UseGas()
        {
            Type baseType = typeof(Nekoyume.Action.ActionBase);
            Type attrType = typeof(ActionTypeAttribute);
            Type obsoleteType = typeof(ActionObsoleteAttribute);

            bool IsTarget(Type type)
            {
                return baseType.IsAssignableFrom(type) &&
                       type.IsDefined(attrType) &&
                       type != typeof(InitializeStates) &&
                       ActionTypeAttribute.ValueOf(type) is { } &&
                       (
                           !type.IsDefined(obsoleteType) ||
                           type
                               .GetCustomAttributes()
                               .OfType<ActionObsoleteAttribute>()
                               .Select(attr => attr.ObsoleteIndex)
                               .FirstOrDefault() > ActionObsoleteConfig.V200030ObsoleteIndex
                       );
            }

            var assembly = baseType.Assembly;
            var typeIds = assembly.GetTypes()
                .Where(IsTarget);
            foreach (var typeId in typeIds)
            {
                var action = (IAction)Activator.CreateInstance(typeId)!;
                var actionContext = new ActionContext
                {
                    PreviousStates = new State(),
                };
                try
                {
                    action.Execute(actionContext);
                }
                catch (Exception)
                {
                    // ignored
                }

                Assert.True(actionContext.GasUsed() > 0, $"{action} not use gas");
            }
        }

        private IAccountStateDelta Execute(IAccountStateDelta state, IAction action, Address signer)
        {
            Assert.True(state.GetBalance(signer, Currencies.Mead) > 0 * Currencies.Mead);
            var nextState = state.BurnAsset(signer, 1 * Currencies.Mead);
            var executedState = action.Execute(new ActionContext
            {
                Signer = signer,
                PreviousStates = nextState,
            });
            return RewardGold.TransferMead(executedState);
        }
    }
}
