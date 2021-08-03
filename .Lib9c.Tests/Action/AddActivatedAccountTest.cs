namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
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
            var state = new State(
                ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
            );
            var newComer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var activatedAddress = newComer.Derive(ActivationKey.DeriveKey);
            if (alreadyActivated)
            {
                state = (State)state.SetState(activatedAddress, true.Serialize());
            }

            var action = new AddActivatedAccount(newComer);
            var signer = isAdmin ? admin : default;

            if (exc is null)
            {
                IAccountStateDelta nextState = action.Execute(new ActionContext()
                {
                    BlockIndex = blockIndex,
                    Miner = default,
                    PreviousStates = state,
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
                    PreviousStates = state,
                    Signer = signer,
                }));
            }
        }

        [Fact]
        public void Rehearsal()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var state = new State(
                ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
            );
            var newComer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var action = new AddActivatedAccount(newComer);

            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousStates = state,
                Signer = admin,
                Rehearsal = true,
            });

            Assert.Equal(
                new[]
                {
                    AdminState.Address,
                    newComer.Derive(ActivationKey.DeriveKey),
                }.ToImmutableHashSet(),
                nextState.UpdatedAddresses
            );
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
