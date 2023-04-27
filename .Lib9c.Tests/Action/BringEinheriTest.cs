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

    public class BringEinheriTest
    {
        [Fact]
        public void Execute()
        {
            Currency mead = Currencies.Mead;
            Address valkyrie = new PrivateKey().ToAddress();
            IAccountStateDelta states = new State().MintAsset(valkyrie, 2 * mead);
            var address = new PrivateKey().ToAddress();
            var action = new BringEinheri
            {
                EinheriAddress = address,
            };

            Assert.Equal(0 * mead, states.GetBalance(address, mead));
            Assert.Equal(2 * mead, states.GetBalance(valkyrie, mead));

            var nextState = action.Execute(new ActionContext
            {
                Signer = valkyrie,
                PreviousStates = states,
            });
            var contract = Assert.IsType<List>(nextState.GetState(address.Derive(nameof(BringEinheri))));

            Assert.Equal(valkyrie, contract[0].ToAddress());
            Assert.False(contract[1].ToBoolean());
            Assert.Equal(1 * mead, nextState.GetBalance(address, mead));
            Assert.Equal(1 * mead, nextState.GetBalance(valkyrie, mead));
        }
    }
}
