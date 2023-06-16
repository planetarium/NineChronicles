namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Libplanet.State;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class CreatePledgeTest
    {
        [Theory]
        [InlineData(true, null)]
        [InlineData(false, typeof(PermissionDeniedException))]
        public void Execute(bool admin, Type exc)
        {
            var adminAddress = new PrivateKey().ToAddress();
            var poolAddress = new PrivateKey().ToAddress();
            var adminState = new AdminState(adminAddress, 150L);
            var patronAddress = new PrivateKey().ToAddress();
            var mead = Currencies.Mead;
            var agentAddress = new PrivateKey().ToAddress();
            IAccountStateDelta states = new State()
                .MintAsset(patronAddress, 4 * mead)
                .SetState(Addresses.Admin, adminState.Serialize());

            var action = new CreatePledge
            {
                PatronAddress = patronAddress,
                Mead = RequestPledge.RefillMead,
                AgentAddresses = new List<Address>
                {
                    agentAddress,
                },
            };

            Address singer = admin ? adminAddress : poolAddress;
            var actionContext = new ActionContext
            {
                Signer = singer,
                PreviousStates = states,
            };

            if (exc is null)
            {
                var nextState = action.Execute(actionContext);

                Assert.Equal(0 * mead, nextState.GetBalance(patronAddress, mead));
                Assert.Equal(4 * mead, nextState.GetBalance(agentAddress, mead));
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(actionContext));
            }
        }
    }
}
