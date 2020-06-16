using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using System.Collections.Immutable;
using Xunit;

namespace Lib9c.Tests.Action
{
    public class ActivateAccountTest
    {
        [Fact]
        public void Execute()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(ActivatedAccountsState.Address, new ActivatedAccountsState().Serialize())
                .Add(pendingActivation.address, pendingActivation.Serialize())
            );

            ActivateAccount action = activationKey.CreateActivateAccount(nonce);
            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = new Address(),
                BlockIndex = 1,
            });

            var activatedAccounts = new ActivatedAccountsState(
                (Dictionary) nextState.GetState(ActivatedAccountsState.Address)
            );
            Assert.Equal(
                new[] { new Address() }.ToImmutableHashSet(),
                activatedAccounts.Accounts
            );
        }

        [Fact]
        public void Rehearsal()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);

            ActivateAccount action = activationKey.CreateActivateAccount(nonce);
            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                PreviousStates = new State(ImmutableDictionary<Address, IValue>.Empty),
                Signer = new Address(),
                Rehearsal = true,
                BlockIndex = 1,
            });

            Assert.Equal(
                ImmutableHashSet.Create(
                    ActivatedAccountsState.Address,
                    pendingActivation.address
                ),
                nextState.UpdatedAddresses
            );
        }

        [Fact]
        public void ExecuteWithInvalidSignature()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(ActivatedAccountsState.Address, new ActivatedAccountsState().Serialize())
                .Add(pendingActivation.address, pendingActivation.Serialize())
            );

            // 잘못된 논스를 넣습니다.
            ActivateAccount action = activationKey.CreateActivateAccount(new byte[] { 0x00, });
            Assert.Throws<InvalidSignatureException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = new Address(),
                    BlockIndex = 1,
                });
            });
        }

        [Fact]
        public void ExecuteWithNonExistsPending()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);

            // state에는 pendingActivation에 해당하는 대기가 없는 상태를 가정합니다.
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(ActivatedAccountsState.Address, new ActivatedAccountsState().Serialize())
            );

            ActivateAccount action = activationKey.CreateActivateAccount(nonce);
            Assert.Throws<PendingActivationDoesNotExistsException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = new Address(),
                    BlockIndex = 1,
                });
            });
        }

        [Fact]
        public void ExecuteWithNonExistsAccounts()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);

            // state가 올바르게 초기화되지 않은 상태를 가정합니다.
            var state = new State(ImmutableDictionary<Address, IValue>.Empty);

            ActivateAccount action = activationKey.CreateActivateAccount(nonce);
            Assert.Throws<ActivatedAccountsDoesNotExistsException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = new Address(),
                    BlockIndex = 1,
                });
            });
        }

        [Fact]
        public void ForbidReusingActivationKey()
        {
            var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            var privateKey = new PrivateKey();
            (ActivationKey activationKey, PendingActivationState pendingActivation) =
                ActivationKey.Create(privateKey, nonce);
            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(ActivatedAccountsState.Address, new ActivatedAccountsState().Serialize())
                .Add(pendingActivation.address, pendingActivation.Serialize())
            );

            ActivateAccount action = activationKey.CreateActivateAccount(nonce);
            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = new Address(),
                BlockIndex = 1,
            });

            Assert.Throws<PendingActivationDoesNotExistsException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = nextState,
                    Signer = new Address("399bddF9F7B6d902ea27037B907B2486C9910730"),
                    BlockIndex = 2,
                });
            });
        }
    }
}
