namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class RequestPledgeTest
    {
        [Fact]
        public void Execute()
        {
            Currency mead = Currencies.Mead;
            Address patron = new PrivateKey().ToAddress();
            IAccountStateDelta states = new State().MintAsset(patron, 2 * mead);
            var address = new PrivateKey().ToAddress();
            var action = new RequestPledge
            {
                AgentAddress = address,
            };

            Assert.Equal(0 * mead, states.GetBalance(address, mead));
            Assert.Equal(2 * mead, states.GetBalance(patron, mead));

            var nextState = action.Execute(new ActionContext
            {
                Signer = patron,
                PreviousStates = states,
            });
            var contract = Assert.IsType<List>(nextState.GetState(address.Derive(nameof(RequestPledge))));

            Assert.Equal(patron, contract[0].ToAddress());
            Assert.False(contract[1].ToBoolean());
            Assert.Equal(1 * mead, nextState.GetBalance(address, mead));
            Assert.Equal(1 * mead, nextState.GetBalance(patron, mead));
        }
    }
}
