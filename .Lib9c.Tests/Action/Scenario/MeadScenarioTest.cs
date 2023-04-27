namespace Lib9c.Tests.Action.Scenario
{
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Xunit;

    public class MeadScenarioTest
    {
        [Fact]
        public void Contract()
        {
            Currency mead = Currencies.Mead;
            var valkyrie = new PrivateKey().ToAddress();
            IAccountStateDelta states = new State().MintAsset(valkyrie, 10 * mead);

            var agentAddress = new PrivateKey().ToAddress();
            var bringEinheri = new BringEinheri
            {
                EinheriAddress = agentAddress,
            };
            var states2 = Execute(states, bringEinheri, valkyrie);
            Assert.Equal(8 * mead, states2.GetBalance(valkyrie, mead));
            Assert.Equal(1 * mead, states2.GetBalance(agentAddress, mead));

            var takeSides = new TakeSides
            {
                ValkyrieAddress = valkyrie,
            };
            var states3 = Execute(states2, takeSides, agentAddress);
            Assert.Equal(7 * mead, states3.GetBalance(valkyrie, mead));
            Assert.Equal(1 * mead, states3.GetBalance(agentAddress, mead));

            // release and return einheri mead
            var releaseEinheri = new ReleaseEinheri
            {
                EinheriAddress = agentAddress,
            };
            var states4 = Execute(states3, releaseEinheri, valkyrie);
            Assert.Equal(7 * mead, states4.GetBalance(valkyrie, mead));
            Assert.Equal(0 * mead, states4.GetBalance(agentAddress, mead));

            // re-contract with Bencodex.Null
            var states5 = Execute(states4, bringEinheri, valkyrie);
            Assert.Equal(5 * mead, states5.GetBalance(valkyrie, mead));
            Assert.Equal(1 * mead, states5.GetBalance(agentAddress, mead));

            var states6 = Execute(states5, takeSides, agentAddress);
            Assert.Equal(4 * mead, states6.GetBalance(valkyrie, mead));
            Assert.Equal(1 * mead, states6.GetBalance(agentAddress, mead));
        }

        private IAccountStateDelta Execute(IAccountStateDelta state, IAction action, Address signer)
        {
            Assert.True(state.GetBalance(signer, Currencies.Mead) > 0 * Currencies.Mead);
            var nextState = state.BurnAsset(signer, 1 * Currencies.Mead);
            return action.Execute(new ActionContext
            {
                Signer = signer,
                PreviousStates = nextState,
            });
        }
    }
}
