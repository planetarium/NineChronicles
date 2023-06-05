namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Xunit;

    public class CreatePledgeTest
    {
        [Fact]
        public void Execute()
        {
            var patronAddress = new PrivateKey().ToAddress();
            var mead = Currencies.Mead;
            var agentAddress = new PrivateKey().ToAddress();
            IAccountStateDelta states = new State()
                .MintAsset(patronAddress, 4 * mead);

            var action = new CreatePledge
            {
                PatronAddress = patronAddress,
                Mead = RequestPledge.RefillMead,
                AgentAddresses = new List<Address>
                {
                    agentAddress,
                },
            };

            var nextState = action.Execute(new ActionContext
            {
                Signer = new PrivateKey().ToAddress(),
                PreviousStates = states,
            });

            Assert.Equal(0 * mead, nextState.GetBalance(patronAddress, mead));
            Assert.Equal(4 * mead, nextState.GetBalance(agentAddress, mead));
        }
    }
}
