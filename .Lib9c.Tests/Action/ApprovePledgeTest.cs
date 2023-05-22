namespace Lib9c.Tests.Action
{
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class ApprovePledgeTest
    {
        [Fact]
        public void Execute()
        {
            var address = new PrivateKey().ToAddress();
            var patron = new PrivateKey().ToAddress();
            var contractAddress = address.Derive(nameof(RequestPledge));
            IAccountStateDelta states = new State()
                .SetState(
                    contractAddress,
                    List.Empty.Add(patron.Serialize()).Add(false.Serialize())
                );

            var action = new ApprovePledge
            {
                PatronAddress = patron,
            };
            var nextState = action.Execute(new ActionContext
            {
                Signer = address,
                PreviousStates = states,
            });

            var contract = Assert.IsType<List>(nextState.GetState(contractAddress));
            Assert.Equal(contract[0].ToAddress(), patron);
            Assert.True(contract[1].ToBoolean());
        }
    }
}
