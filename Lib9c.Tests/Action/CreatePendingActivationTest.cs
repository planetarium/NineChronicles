namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class CreatePendingActivationTest
    {
        [Fact]
        public void Execute()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var pubKey = new PublicKey(
                ByteUtil.ParseHex("02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1")
            );
            var pendingActivation = new PendingActivationState(nonce, pubKey);
            var action = new CreatePendingActivation(pendingActivation);
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, adminState.Serialize())
            );
            var actionContext = new ActionContext()
            {
                BlockIndex = 1,
                PreviousStates = state,
                Signer = adminAddress,
            };

            var nextState = action.Execute(actionContext);
            Assert.Equal(
                pendingActivation.Serialize(),
                nextState.GetState(pendingActivation.address)
            );
        }

        [Fact]
        public void CheckPermission()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var pubKey = new PublicKey(
                ByteUtil.ParseHex("02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1")
            );
            var pendingActivation = new PendingActivationState(nonce, pubKey);
            var action = new CreatePendingActivation(pendingActivation);
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(AdminState.Address, adminState.Serialize())
            );

            Assert.Throws<PolicyExpiredException>(
                () => action.Execute(new ActionContext()
                {
                    BlockIndex = 101,
                    PreviousStates = state,
                    Signer = adminAddress,
                })
            );

            Assert.Throws<PermissionDeniedException>(
                () => action.Execute(new ActionContext()
                {
                    BlockIndex = 1,
                    PreviousStates = state,
                    Signer = default,
                })
            );
        }

        [Fact]
        public void Rehearsal()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var pubKey = new PublicKey(
                ByteUtil.ParseHex("02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1")
            );
            var pendingActivation = new PendingActivationState(nonce, pubKey);
            var action = new CreatePendingActivation(pendingActivation);
            IAccountStateDelta nextState = action.Execute(
                new ActionContext()
                {
                    BlockIndex = 101,
                    Signer = default,
                    Rehearsal = true,
                    PreviousStates = new State(ImmutableDictionary<Address, IValue>.Empty),
                }
            );
            Assert.Equal(
                ImmutableHashSet.Create(pendingActivation.address),
                nextState.UpdatedAddresses
            );
        }
    }
}
