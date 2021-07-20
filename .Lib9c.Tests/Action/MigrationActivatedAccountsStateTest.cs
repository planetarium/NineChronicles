namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Xunit;

    public class MigrationActivatedAccountsStateTest
    {
        [Fact]
        public void Execute()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var privateKey = new PrivateKey();
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
                .Add(ActivatedAccountsState.Address, new ActivatedAccountsState().AddAccount(default).Serialize())
            );

            var action = new MigrationActivatedAccountsState
            {
                Addresses = new List<Address>
                {
                    default,
                },
            };

            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = admin,
                BlockIndex = 1,
            });

            var nextAccountsState = new ActivatedAccountsState(
                (Dictionary)nextState.GetState(ActivatedAccountsState.Address)
            );
            Assert.Empty(nextAccountsState.Accounts);
            Assert.True(nextState.GetState(default(Address).Derive(ActivationKey.DeriveKey)).ToBoolean());
        }
    }
}
