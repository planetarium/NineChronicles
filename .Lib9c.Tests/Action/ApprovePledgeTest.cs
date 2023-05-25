namespace Lib9c.Tests.Action
{
    using System;
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

        [Theory]
        [InlineData(false, false, typeof(FailedLoadStateException))]
        [InlineData(true, false, typeof(InvalidAddressException))]
        [InlineData(false, true, typeof(AlreadyContractedException))]
        public void Execute_Throw_Exception(bool invalidPatron, bool alreadyContract, Type exc)
        {
            var address = new PrivateKey().ToAddress();
            var patron = new PrivateKey().ToAddress();
            var contractAddress = address.Derive(nameof(RequestPledge));
            IValue contract = Null.Value;
            if (invalidPatron)
            {
                contract = List.Empty.Add(new PrivateKey().ToAddress().Serialize());
            }

            if (alreadyContract)
            {
                contract = List.Empty.Add(patron.Serialize()).Add(true.Serialize());
            }

            IAccountStateDelta states = new State().SetState(contractAddress, contract);

            var action = new ApprovePledge
            {
                PatronAddress = patron,
            };
            Assert.Throws(exc, () => action.Execute(new ActionContext
            {
                Signer = address,
                PreviousStates = states,
            }));
        }
    }
}
