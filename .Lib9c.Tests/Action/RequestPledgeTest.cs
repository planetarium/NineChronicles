namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class RequestPledgeTest
    {
        [Theory]
        [InlineData(RequestPledge.DefaultRefillMead)]
        [InlineData(100)]
        public void Execute(int contractedMead)
        {
            Currency mead = Currencies.Mead;
            Address patron = new PrivateKey().ToAddress();
            var context = new ActionContext();
            IAccountStateDelta states = new MockStateDelta().MintAsset(context, patron, 2 * mead);
            var address = new PrivateKey().ToAddress();
            var action = new RequestPledge
            {
                AgentAddress = address,
                RefillMead = contractedMead,
            };

            Assert.Equal(0 * mead, states.GetBalance(address, mead));
            Assert.Equal(2 * mead, states.GetBalance(patron, mead));

            var nextState = action.Execute(new ActionContext
            {
                Signer = patron,
                PreviousState = states,
            });
            var contract = Assert.IsType<List>(nextState.GetState(address.GetPledgeAddress()));

            Assert.Equal(patron, contract[0].ToAddress());
            Assert.False(contract[1].ToBoolean());
            Assert.Equal(contractedMead, contract[2].ToInteger());
            Assert.Equal(1 * mead, nextState.GetBalance(address, mead));
            Assert.Equal(1 * mead, nextState.GetBalance(patron, mead));
        }

        [Fact]
        public void Execute_Throw_AlreadyContractedException()
        {
            Address patron = new PrivateKey().ToAddress();
            var address = new PrivateKey().ToAddress();
            Address contractAddress = address.GetPledgeAddress();
            IAccountStateDelta states = new MockStateDelta().SetState(contractAddress, List.Empty);
            var action = new RequestPledge
            {
                AgentAddress = address,
                RefillMead = 1,
            };

            Assert.Throws<AlreadyContractedException>(() => action.Execute(new ActionContext
            {
                Signer = patron,
                PreviousState = states,
            }));
        }
    }
}
