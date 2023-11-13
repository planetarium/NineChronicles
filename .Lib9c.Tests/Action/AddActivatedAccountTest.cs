namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Immutable;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Xunit;

    public class AddActivatedAccountTest
    {
        [Theory]
        [InlineData(true, 1, false, null)]
        [InlineData(true, 101, false, typeof(PolicyExpiredException))]
        [InlineData(false, 1, false, typeof(PermissionDeniedException))]
        [InlineData(true, 1, true, typeof(AlreadyActivatedException))]
        public void Execute(bool isAdmin, long blockIndex, bool alreadyActivated, Type exc)
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            IAccount state = new Account(
                MockState.Empty
                    .SetState(AdminState.Address, new AdminState(admin, 100).Serialize()));
            var newComer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var activatedAddress = newComer.Derive(ActivationKey.DeriveKey);
            if (alreadyActivated)
            {
                state = state.SetState(activatedAddress, true.Serialize());
            }

            var action = new AddActivatedAccount(newComer);
            var signer = isAdmin ? admin : default;

            if (exc is null)
            {
                IAccount nextState = action.Execute(new ActionContext()
                {
                    BlockIndex = blockIndex,
                    Miner = default,
                    PreviousState = state,
                    Signer = signer,
                });
                Assert.True(nextState.GetState(activatedAddress).ToBoolean());
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext()
                {
                    BlockIndex = blockIndex,
                    Miner = default,
                    PreviousState = state,
                    Signer = signer,
                }));
            }
        }

        [Fact]
        public void PlainValue()
        {
            var newComer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var action = new AddActivatedAccount(newComer);
            var action2 = new AddActivatedAccount();
            action2.LoadPlainValue(action.PlainValue);

            Assert.Equal(action.Address, action2.Address);
        }
    }
}
