namespace Lib9c.Tests.Action
{
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class AddActivatedAccount0Test
    {
        [Fact]
        public void Execute()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var state = new State(
                ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
                .Add(ActivatedAccountsState.Address, new ActivatedAccountsState().Serialize())
            );
            var newComer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var action = new AddActivatedAccount0(newComer);

            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousStates = state,
                Signer = admin,
            });

            var nextAccountStates = new ActivatedAccountsState(
                (Dictionary)nextState.GetState(ActivatedAccountsState.Address)
            );

            Assert.Equal(
                ImmutableHashSet.Create(newComer),
                nextAccountStates.Accounts
            );
        }

        [Fact]
        public void Rehearsal()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var state = new State(
                ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
                .Add(ActivatedAccountsState.Address, new ActivatedAccountsState().Serialize())
            );
            var newComer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var action = new AddActivatedAccount0(newComer);

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
                    ActivatedAccountsState.Address,
                }.ToImmutableHashSet(),
                nextState.UpdatedAddresses
            );
        }

        [Fact]
        public void ExecuteWithNonExistsAccounts()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var state = new State(ImmutableDictionary<Address, IValue>.Empty);
            var newComer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var action = new AddActivatedAccount0(newComer);

            Assert.Throws<ActivatedAccountsDoesNotExistsException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    BlockIndex = 1,
                    Miner = default,
                    PreviousStates = state,
                    Signer = admin,
                });
            });
        }

        [Fact]
        public void CheckPermission()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var state = new State(
                ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
                .Add(ActivatedAccountsState.Address, new ActivatedAccountsState().Serialize())
            );
            var newComer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var action = new AddActivatedAccount0(newComer);

            Assert.Throws<PermissionDeniedException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    BlockIndex = 1,
                    Miner = default,
                    PreviousStates = state,
                    Signer = newComer,
                });
            });

            Assert.Throws<PolicyExpiredException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    BlockIndex = 101,
                    Miner = default,
                    PreviousStates = state,
                    Signer = admin,
                });
            });
        }
    }
}
