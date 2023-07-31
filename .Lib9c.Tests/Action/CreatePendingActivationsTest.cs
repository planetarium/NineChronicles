namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class CreatePendingActivationsTest
    {
        [Fact]
        public void Execute()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            PendingActivationState CreatePendingActivation()
            {
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
                var pubKey = new PrivateKey().PublicKey;
                return new PendingActivationState(nonce, pubKey);
            }

            PendingActivationState[] activations =
                Enumerable.Range(0, 5000).Select(_ => CreatePendingActivation()).ToArray();
            var action = new CreatePendingActivations(activations);
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var state = new MockStateDelta(
                MockState.Empty
                    .SetState(AdminState.Address, adminState.Serialize()));
            var actionContext = new ActionContext()
            {
                BlockIndex = 1,
                PreviousState = state,
                Signer = adminAddress,
            };

            var nextState = action.Execute(actionContext);

            foreach (PendingActivationState pa in activations)
            {
                Assert.Equal(
                    pa.Serialize(),
                    nextState.GetState(pa.address)
                );
            }
        }

        [Fact]
        public void PlainValue()
        {
            byte[] nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            PublicKey pubKey = new PrivateKey().PublicKey;
            Address address = PendingActivationState.DeriveAddress(nonce, pubKey);
            var plainValue = Dictionary.Empty
                .Add("type_id", "create_pending_activations")
                .Add("values", new List()
                    .Add(new List(address.Serialize(), (Binary)nonce, pubKey.Serialize())));

            var action = new CreatePendingActivations();
            action.LoadPlainValue(plainValue);

            var plainValueFromAction = Assert.IsType<Dictionary>(action.PlainValue);
            Assert.Equal((Text)"create_pending_activations", plainValueFromAction["type_id"]);
            var valuesFromAction = Assert.IsType<List>(plainValueFromAction["values"]);
            var activationFromAction = Assert.IsType<List>(valuesFromAction[0]);

            Assert.Equal(address, new Address(activationFromAction[0]));
            Assert.Equal(nonce, (Binary)activationFromAction[1]);
            Assert.Equal(pubKey, new PublicKey(((Binary)activationFromAction[2]).ByteArray));
        }

        [Fact]
        public void CheckPermission()
        {
            var action = new CreatePendingActivations();
            var adminAddress = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var adminState = new AdminState(adminAddress, 100);
            var state = new MockStateDelta(
                MockState.Empty
                    .SetState(AdminState.Address, adminState.Serialize()));

            Assert.Throws<PolicyExpiredException>(
                () => action.Execute(new ActionContext()
                {
                    BlockIndex = 101,
                    PreviousState = state,
                    Signer = adminAddress,
                })
            );

            Assert.Throws<PermissionDeniedException>(
                () => action.Execute(new ActionContext()
                {
                    BlockIndex = 1,
                    PreviousState = state,
                    Signer = default,
                })
            );
        }
    }
}
